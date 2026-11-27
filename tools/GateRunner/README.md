# GateRunner

Drives the real API and scripted Simulated Room, retaining content-free hop
timings and trace/span ids. It also evaluates authored synthetic utterances
through the real decision adapter and the same Domain grounding/rules.
References Application, Domain and Infrastructure; never writes Azure DevOps
from the evaluation path.

Run the timing harness as documented in `docs/DEMO.md`; decision evaluation and
its blocking release command are in `demo/evaluation/README.md`. Reports belong
outside committed source. Adapter tests are in Infrastructure.Tests; real
approval traces are checked through the Application mediator tests.
