# Agent Evaluation Demo

This demo showcases an instrumented agentic system with evaluation capabilities using Langfuse datasets and OpenTelemetry tracing.

## Available Evaluators

To list all available evaluators:
```bash
dotnet run -- evaluate --list-evaluators
```

### NLP Evaluators (for text output comparison)
- **`bleu`** - BLEU score measuring n-gram overlap between output and reference (0.0 = no match, 1.0 = perfect match)
- **`gleu`** - Google BLEU variant optimized for sentence-level comparison (0.0 = no match, 1.0 = perfect match)
- **`f1`** - F1 score measuring word overlap between output and reference (0.0 = no overlap, 1.0 = exact match)

### Trajectory Evaluators (for agent tool call sequences)
- **`trajectory_strict`** - Verifies exact sequence of tool calls matches expected trajectory (order matters)
- **`trajectory_unordered`** - Verifies all expected tools were called with correct counts (order doesn't matter)

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