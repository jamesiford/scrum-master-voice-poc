import { createRequire } from "node:module";
import { readFile, mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { createHash } from "node:crypto";

/**
 * Generates independent golden PNGs with the unmodified browser renderer, never with the native port.
 * Run from the repository root: node src\tests\Zava.Scrum.MediaBot.Tests\BoardGoldens\generate-chromium.mjs.
 * Requires the bridge's existing npm dependencies and local Edge, just like its existing e2e runner.
 */
const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "..", "..", "..", "..");
const requireBridge = createRequire(
  resolve(root, "src", "meeting-bridge", "package.json"),
);
const { chromium } = requireBridge("playwright-core");
const { transform } = requireBridge("esbuild");
const sourcePath = resolve(
  root,
  "src",
  "meeting-bridge",
  "src",
  "web",
  "board.ts",
);
const source = await readFile(sourcePath, "utf8");
const fixtures = JSON.parse(
  await readFile(resolve(here, "fixtures.json"), "utf8"),
);
const { code } = await transform(source, {
  loader: "ts",
  format: "iife",
  globalName: "BoardModule",
});
const browser = await chromium.launch({ channel: "msedge", headless: true });
const baselinePath = resolve(here, "chromium");
await mkdir(baselinePath, { recursive: true });
const results = [];

try {
  const page = await browser.newPage({
    viewport: { width: 1280, height: 720 },
    locale: "en-US",
    deviceScaleFactor: 1,
  });
  await page.setContent("<!doctype html><body></body>");
  // Only clock and repaint scheduling are fixed. The actual board.ts paints every baseline.
  await page.evaluate(() => {
    Date.prototype.toLocaleTimeString = () => "1:23:45 PM";
    window.setInterval = () => 0;
  });
  await page.addScriptTag({ content: code });
  for (const fixture of fixtures) {
    const png = await page.evaluate(({ state }) => {
      document.querySelectorAll("canvas").forEach((canvas) => canvas.remove());
      const renderer = new BoardModule.BoardRenderer();
      if (state !== null) renderer.update(state);
      return document
        .querySelector("canvas")
        .toDataURL("image/png")
        .split(",")[1];
    }, fixture);
    const bytes = Buffer.from(png, "base64");
    await writeFile(resolve(baselinePath, `${fixture.name}.png`), bytes);
    results.push({
      name: fixture.name,
      sha256: createHash("sha256").update(bytes).digest("hex"),
    });
  }
  await writeFile(
    resolve(baselinePath, "provenance.json"),
    JSON.stringify(
      {
        generator:
          "Actual unchanged meeting-bridge board.ts, bundled with esbuild and painted on an Edge Chromium canvas",
        browser: browser.version(),
        platform: process.platform,
        clock: "1:23:45 PM",
        locale: "en-US",
        sourceSha256: createHash("sha256").update(source).digest("hex"),
        fixturesSha256: createHash("sha256")
          .update(await readFile(resolve(here, "fixtures.json")))
          .digest("hex"),
        fixtures: results,
      },
      null,
      2,
    ) + "\n",
  );
  console.log(
    `Generated ${results.length} genuine Chromium baselines (${browser.version()}).`,
  );
} finally {
  await browser.close();
}
