namespace MudBlazor.UnitTests.Docs.Generator;

public class TestsForExamples
{
    public bool Execute()
    {
        var success = true;
        try
        {
            Directory.CreateDirectory(Paths.TestDirPath);

            var currentCode = string.Empty;
            if (File.Exists(Paths.ComponentTestsFilePath))
            {
                currentCode = File.ReadAllText(Paths.ComponentTestsFilePath);
            }

            var cb = new CodeBuilder();

            cb.AddHeader();
            cb.AddLine("using System;");
            cb.AddLine("using Microsoft.Extensions.DependencyInjection;");
            cb.AddLine("using MudBlazor.Docs.Examples;");
            cb.AddLine("using MudBlazor.Docs.Wireframes;");
            cb.AddLine("using MudBlazor.Services;");
            cb.AddLine("using NUnit.Framework;");
            cb.AddLine();

            cb.AddLine("namespace MudBlazor.UnitTests.Docs.Generated");
            cb.AddLine("{");
            cb.IndentLevel++;
            cb.AddLine("// These tests just check if all the examples from the doc page render without errors");
            cb.AddLine("[System.CodeDom.Compiler.GeneratedCodeAttribute(\"MudBlazor.Docs.Compiler\", \"0.0.0.0\")]");
            cb.AddLine("public partial class ExampleDocsTests");
            cb.AddLine("{");
            cb.IndentLevel++;
            var exampleComponents = new List<string>();

            foreach (var entry in Directory.EnumerateFiles(Paths.DocsDirPath, "*.razor", SearchOption.AllDirectories)
                .OrderBy(e => e.Replace("\\", "/"), StringComparer.Ordinal))
            {
                if (entry.EndsWith("Code.razor"))
                    continue;
                var filename = Path.GetFileName(entry);
                var componentName = Path.GetFileNameWithoutExtension(filename);
                if (!filename.Contains(Paths.ExampleDiscriminator))
                    continue;
                // skip over table/data grid virtualization since it takes too long.
                if (filename == "TableVirtualizationExample.razor" || filename == "DataGridVirtualizationExample.razor")
                    continue;
                exampleComponents.Add(componentName);
            }

            cb.AddLine("private static readonly Type[][] ExampleComponentBatches =");
            cb.AddLine("[");
            cb.IndentLevel++;
            const int batchSize = 12;
            for (var i = 0; i < exampleComponents.Count; i += batchSize)
            {
                cb.AddLine("[");
                cb.IndentLevel++;
                foreach (var componentName in exampleComponents.Skip(i).Take(batchSize))
                {
                    cb.AddLine($"typeof({componentName}),");
                }
                cb.IndentLevel--;
                cb.AddLine("],");
            }
            cb.IndentLevel--;
            cb.AddLine("];");
            cb.AddLine();
            cb.AddLine("[TestCaseSource(nameof(ExampleComponentBatches))]");
            cb.AddLine("public async Task Examples_Render_Without_Errors(Type[] components)");
            cb.AddLine("{");
            cb.IndentLevel++;
            cb.AddLine("await using var context = CreateContext();");
            cb.AddLine("foreach (var componentType in components)");
            cb.AddLine("{");
            cb.IndentLevel++;
            cb.AddLine("context.RenderInsideRenderTree(builder =>");
            cb.AddLine("{");
            cb.IndentLevel++;
            cb.AddLine("builder.OpenComponent(0, componentType);");
            cb.AddLine("builder.CloseComponent();");
            cb.IndentLevel--;
            cb.AddLine("});");
            cb.AddLine("await context.Services.GetRequiredService<IRenderQueueService>().WaitUntilEmpty();");
            cb.IndentLevel--;
            cb.AddLine("}");
            cb.IndentLevel--;
            cb.AddLine("}");

            cb.IndentLevel--;
            cb.AddLine("}");
            cb.IndentLevel--;
            cb.AddLine("}");

            if (currentCode != cb.ToString())
            {
                File.WriteAllText(Paths.ComponentTestsFilePath, cb.ToString());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(@$"Error generating {Paths.ComponentTestsFilePath} : {e.Message}");
            success = false;
        }

        return success;
    }
}
