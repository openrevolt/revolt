using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace RevoltMacLookupGenerator;

[Generator]
public class Generator : IIncrementalGenerator {

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        IncrementalValuesProvider<ClassDeclarationSyntax> provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (d, _)=> d is ClassDeclarationSyntax,
            transform: (n, _)=> (ClassDeclarationSyntax)n.Node
        ).Where(m=> m is not null);

        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right)> compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left));
    }

    private void Execute(SourceProductionContext context, Compilation compilation) {
        string rootPath = compilation.SyntaxTrees
            .Where(o => o.FilePath.EndsWith($"Revolt{Path.DirectorySeparatorChar}Program.cs"))
            .First().FilePath
            .ToString();

        rootPath = $"{rootPath.Substring(0, rootPath.Length - 10)}";

        StringBuilder builder = new StringBuilder();

        builder.AppendLine("namespace Revolt.Protocols;");
        builder.AppendLine("public static partial class MacLookup {");

        builder.AppendLine("    public static readonly (byte, byte, string)[][] table = [");
        LoadFile(rootPath, builder);
        builder.AppendLine("    ];");

        builder.AppendLine("}");

        context.AddSource("MacLookup.g.cs", builder.ToString());
    }

    private static void LoadFile(string rootPath, StringBuilder builder) {
        string filename = $"{rootPath}data{Path.DirectorySeparatorChar}oui.txt";

        builder.AppendLine();
        builder.AppendLine("    //looking for: " + filename);
        builder.AppendLine();

        FileInfo file = new FileInfo(filename);
        if (!file.Exists) return;

        using FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using StreamReader reader = new StreamReader(stream);

        List<(byte, byte, string)>[] array = new List<(byte, byte, string)>[256];
        for (int i = 0; i < 256; i++) {
            array[i] = [];
        }

        string line;
        while ((line = reader.ReadLine()) is not null) {
            if (line.Length < 16) continue;
            if (line[2] != '-' || line[5] != '-') continue;

            byte a = byte.Parse(line.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(line.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            byte c = byte.Parse(line.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            string vendor = line.Substring(18).Trim();

            array[a].Add((b, c, vendor));
        }

        for (int i = 0; i < 256; i++) {
            array[i].Sort((a, b)=> {
                int comp = a.Item1.CompareTo(b.Item1);
                return comp == 0 ? a.Item2.CompareTo(b.Item2) : comp;
            });
        }

        for (int i = 0; i < 256; i++) {
            builder.AppendLine("    [");
            foreach (var (b, c, vendor) in array[i]) {
                builder.AppendLine($"    ({b}, {c}, \"{vendor}\"),");
            }
            builder.AppendLine("    ],");
        }

    }

}
