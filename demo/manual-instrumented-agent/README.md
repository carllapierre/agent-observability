# Agent Evaluation Demo

This demo showcases an instrumented agentic system with evaluation capabilities using Langfuse datasets and OpenTelemetry tracing.

## Available Evaluators

To list all available evaluators:
```bash
dotnet run -- evaluate --list-evaluators
```

## Datasets

### RAG Dataset
Tests retrieval-augmented generation. 

**Run experiment:**
```bash
dotnet run -- run-experiment --dataset "RetrievalDataset"
```

**Run evaluations (defaults to latest run):**
```bash
dotnet run -- evaluate --dataset "RetrievalDataset"
```

Or specify a run name and evaluators:
```bash
dotnet run -- evaluate --dataset "RetrievalDataset" --run "your-run-name" --evaluators bleu,gleu,f1
```

---

## Chat Mode

Run the agent interactively:
```bash
dotnet run -- chat
```