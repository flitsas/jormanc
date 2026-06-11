using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Domain.Services;
using Xunit;

namespace Flit.Documents.Tests.Services;

public class DocumentCompletionCheckerTests
{
    [Fact]
    public void AC1_RechazaConsolidacionSiFaltaObligatorio()
    {
        var docTypeId = Guid.NewGuid();
        var configs = new List<ProcedureTypeDocument>
        {
            new()
            {
                DocumentTypeId = docTypeId,
                IsRequired = true,
                AllowPartialConsolidation = false,
                DocumentType = new DocumentType { Name = "Contrato" }
            }
        };

        var documents = new List<ProcedureDocument>
        {
            new()
            {
                DocumentTypeId = docTypeId,
                Status = "pending"
            }
        };

        DocumentCompletionChecker.CanConsolidate(configs, documents).Should().BeFalse();
    }

    [Fact]
    public void AC1_PermiteConsolidacionParcial()
    {
        var requiredId = Guid.NewGuid();
        var optionalId = Guid.NewGuid();

        var configs = new List<ProcedureTypeDocument>
        {
            new()
            {
                DocumentTypeId = requiredId,
                IsRequired = true,
                AllowPartialConsolidation = true,
                DocumentType = new DocumentType { Name = "A" }
            },
            new()
            {
                DocumentTypeId = optionalId,
                IsRequired = false,
                AllowPartialConsolidation = true,
                DocumentType = new DocumentType { Name = "B" }
            }
        };

        var documents = new List<ProcedureDocument>
        {
            new()
            {
                DocumentTypeId = optionalId,
                Status = "ready",
                FileRef = "docs/b.pdf"
            }
        };

        DocumentCompletionChecker.CanConsolidate(configs, documents).Should().BeTrue();
    }
}
