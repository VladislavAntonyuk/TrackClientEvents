﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Entities.SourceGenerators;

public static class SourceGenerationHelper
{
    public const string Attribute = @"// <auto-generated>
namespace Entities;

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
public class AuditAttribute : System.Attribute
{
}";

    private const string AuditableProperties = @"
            public Guid? AuditUserId { get; set; }
            public int? AuditClientId { get; set; }
            public DateTime AuditDate { get; set; }
            public string AuditActionType { get; set; }";

    public const string AuditClass = @$"// <auto-generated>
using System;

namespace Entities;

public abstract class Auditable
{{
   {AuditableProperties}
}}";

    public static (string, string, string)? GetAuditableClassCode(GeneratorExecutionContext context,
        TypeDeclarationSyntax typeNode)
    {
        var codeBuilder = new StringBuilder();
        var typeNodeSymbol = context.Compilation
            .GetSemanticModel(typeNode.SyntaxTree)
            .GetDeclaredSymbol(typeNode);

        if (typeNodeSymbol is null) return null;

        var entityClassNamespace = typeNodeSymbol.ContainingNamespace?.ToDisplayString() ?? "Entities";

        var generatedClassName = $"{typeNodeSymbol.Name}Auditable";

        codeBuilder.AppendLine("// <auto-generated>");
        codeBuilder.AppendLine("using System;");
        codeBuilder.AppendLine("using System.Collections.Generic;");
        codeBuilder.AppendLine("using System.Linq;");
        codeBuilder.AppendLine("using Microsoft.EntityFrameworkCore;");
        codeBuilder.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        codeBuilder.AppendLine("using TrackClientEventsAuditNetWithSourceGenerators;");

        codeBuilder.AppendLine("");
        codeBuilder.AppendLine($"namespace {entityClassNamespace};");
        codeBuilder.AppendLine("");

        codeBuilder.AppendLine($"public class {generatedClassName}: Entities.Auditable");
        codeBuilder.AppendLine("{");

        codeBuilder.AppendLine("\tpublic int Identifier { get; set; }");
        var allProperties = typeNode.Members.OfType<PropertyDeclarationSyntax>()
            .Where(x => x.Modifiers.All(y => y.Text != "virtual"));
        foreach (var property in allProperties)
            codeBuilder.AppendLine($"\t{property.BuildProperty(context.Compilation)}");

        codeBuilder.AppendLine("}");

        codeBuilder.AppendLine(
            @$"public partial class {generatedClassName}Configuration : IEntityTypeConfiguration<{generatedClassName}>
{{
    public void Configure(EntityTypeBuilder<{generatedClassName}> builder)
    {{
            builder.HasKey(x => x.Identifier);");

        codeBuilder.AppendLine(@"
            }
}");
        return (codeBuilder.ToString(), typeNodeSymbol.Name, generatedClassName);
    }

    public static string GenerateDbContext(List<string> types)
    {
        var dbContextCode = new StringBuilder();
        dbContextCode.AppendLine("// <auto-generated>");
        dbContextCode.AppendLine("using Microsoft.EntityFrameworkCore;");
        dbContextCode.AppendLine("using Entities;");
        dbContextCode.AppendLine("");
        dbContextCode.AppendLine("namespace TrackClientEventsAuditNetWithSourceGenerators;");
        dbContextCode.AppendLine("");
        dbContextCode.AppendLine("public partial class ClientDbContext");
        dbContextCode.AppendLine("{");
        foreach (var type in types) dbContextCode.AppendLine($"\tpublic virtual DbSet<{type}> {type}s {{ get; set; }}");

        dbContextCode.AppendLine("protected void OnAuditModelCreating(ModelBuilder modelBuilder)");
        dbContextCode.AppendLine("{");
        foreach (var type in types)
            dbContextCode.AppendLine($"\tmodelBuilder.ApplyConfiguration(new {type}Configuration());");

        dbContextCode.AppendLine("}");

        dbContextCode.AppendLine("}");
        return dbContextCode.ToString();
    }

    public static string GenerateAuditMappingExtensions(
        List<(string originalClassName, string generatedClassName)> types)
    {
        var dbContextCode = new StringBuilder();
        dbContextCode.AppendLine("// <auto-generated>");
        dbContextCode.AppendLine("using Audit.EntityFramework.ConfigurationApi;");
        dbContextCode.AppendLine("using TrackClientEventsAuditNetWithSourceGenerators;");
        dbContextCode.AppendLine("");
        dbContextCode.AppendLine("namespace Entities;");
        dbContextCode.AppendLine("");
        dbContextCode.AppendLine("public static class AuditMappingExtensions");
        dbContextCode.AppendLine("{");

        dbContextCode.AppendLine("public static IAuditEntityMapping MapAuditables(this IAuditEntityMapping map)");
        dbContextCode.AppendLine("{");
        foreach (var (originalClassName, generatedClassName) in types)
            dbContextCode.AppendLine($"\tmap.Map<{originalClassName}, {generatedClassName}>();");

        dbContextCode.AppendLine("\treturn map;");
        dbContextCode.AppendLine("}");

        dbContextCode.AppendLine("}");
        return dbContextCode.ToString();
    }
}