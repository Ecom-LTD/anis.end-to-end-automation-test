using Xunit;

// xUnit يشغّل المجموعات بالتوازي فيما بينها؛ كل منتج في Collection خاص به.
[assembly: CollectionBehavior(MaxParallelThreads = 8)]
