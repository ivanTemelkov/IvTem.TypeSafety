using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace IvTem.TypeSafety.Analysis;

internal sealed class DiagnosticDeduplicator
{
    private readonly ConcurrentDictionary<DiagnosticKey, byte> reportedDiagnostics = new();

    public bool TryMarkReported(Location location, int typeArgumentOrdinal)
    {
        var sourceTree = location.SourceTree;
        if (sourceTree is null)
            return true;

        var key = new DiagnosticKey(sourceTree, location.SourceSpan, typeArgumentOrdinal);
        return reportedDiagnostics.TryAdd(key, 0);
    }

    private sealed class DiagnosticKey
    {
        public DiagnosticKey(SyntaxTree sourceTree, TextSpan sourceSpan, int typeArgumentOrdinal)
        {
            SourceTree = sourceTree;
            SourceSpan = sourceSpan;
            TypeArgumentOrdinal = typeArgumentOrdinal;
        }

        private SyntaxTree SourceTree { get; }

        private TextSpan SourceSpan { get; }

        private int TypeArgumentOrdinal { get; }

        public override bool Equals(object? obj)
            => obj is DiagnosticKey other
                && ReferenceEquals(SourceTree, other.SourceTree)
                && SourceSpan == other.SourceSpan
                && TypeArgumentOrdinal == other.TypeArgumentOrdinal;

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + SourceTree.GetHashCode();
                hash = (hash * 31) + SourceSpan.GetHashCode();
                hash = (hash * 31) + TypeArgumentOrdinal.GetHashCode();
                return hash;
            }
        }
    }
}
