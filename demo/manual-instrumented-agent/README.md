# Agent Evaluation Demo

This demo showcases an instrumented agentic system with evaluation capabilities using Langfuse datasets and OpenTelemetry tracing.

## Available Evaluators

To list all available evaluators:
```bash
dotnet run -- evaluate --list-evaluators
```

## Eval 1: Feedback Scoring
When running a chat, we pair each message with a trace ID. These ids can be scored manually via user feedback. In the chat, typing the keyword 'bad' enters the feedback state. Then you can write any message to tag the last trace with a feedback score and comment. 

```bash
dotnet run
```

## Running An Experiment
The next evals will be tied to an experiment run. Experiments can be run against a dataset using the experiment runner command. This will iterate over the dataset and run the agent against a list of inputs. Trace ids will be saved as run items in an experiment for subsequent evaluations.

```bash
dotnet run -- run-experiment --dataset "RetrievalDataset"
``` 

## Eval 2: NLP

NLP evals are rule-based measures
that are computed by comparing model outputs to ground-truth references. They are based on counts, probabilities, or overlaps and are designed to be objective and reproducible. This project implements 3 NLP evals, BLEU, GLEU and F1. 

```bash
dotnet run -- evaluate --dataset "RetrievalDataset" --evaluators bleu,gleu,f1
```

## Eval 3: Trajectory

Trajectory evals ensure the agent called the exact or subset of tools listed in the dataset metadata. In this case we have 2 evals, one that follows a strict order of tools and another unordered. 

```bash
dotnet run -- evaluate --dataset "RetrievalDataset" --evaluators trajectory_strict,trajectory_unordered
```

## Eval 4: RAG Triad

The RAG triad evals are a set of 3 evals that tackle different aspects of retrieval augmented generation without solely relying on end results. We evaluate answer and context relevance as well as groundedness. 

```bash
dotnet run -- evaluate --dataset "RetrievalDataset" --evaluators   triad-answer-relevance, triad-context-relevance, triad-groundedness
```

## Eval 5: All of them!

After making our changes to the agent, re-run the experiment and run all the evaluators. 

```bash
dotnet run -- run-experiment --dataset "RetrievalDataset"
```
```bash
dotnet run -- evaluate --dataset "RetrievalDataset"
```