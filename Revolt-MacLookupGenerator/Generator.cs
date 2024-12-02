using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Revolt_MacLookupGenerator;

[Generator]
internal class Generator : IIncrementalGenerator {

    static readonly string filename = "test.csv";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        FileInfo file = new FileInfo(filename);
        if (!file.Exists) return;

        using FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
        using BinaryReader br = new BinaryReader(fs);
        byte[] bytes = br.ReadBytes((int)file.Length);

    }
}
