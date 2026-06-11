using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Procedures.Tests.Commands;

/// <summary>AC3 HU-9786 — POST /procedures/{id}/attachments</summary>
public class UploadAttachmentCommandHandlerTests
{
    private readonly IProcedureRepository _procedureRepo = Substitute.For<IProcedureRepository>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly UploadAttachmentCommandHandler _sut;
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 15, 45, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();

    public UploadAttachmentCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new UploadAttachmentCommandHandler(_procedureRepo, _fileStorage, _clock);
    }

    [Fact]
    public async Task AC3_UploadAdjunto_PersisteConFileRefMinIO()
    {
        var procedure = new Procedure { Id = ProcedureId, TenantId = TenantId, Status = "draft" };
        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);

        var expectedKey = $"procedures/{TenantId}/{ProcedureId}/licencia_transito/doc.pdf";
        _fileStorage.UploadAsync(expectedKey, Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>())
            .Returns(expectedKey);

        await using var content = new MemoryStream([0x25, 0x50, 0x44, 0x46]);
        var command = new UploadAttachmentCommand(
            ProcedureId, TenantId, UserId, "licencia_transito", "doc.pdf", "application/pdf", 4, content);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("doc.pdf");
        result.Value.LabelSlug.Should().Be("licencia_transito");

        await _procedureRepo.Received(1).AddAttachmentAsync(
            Arg.Is<ProcedureAttachment>(a =>
                a.FileRef == expectedKey &&
                a.LabelSlug == "licencia_transito" &&
                a.UploadedBy == UserId),
            Arg.Any<CancellationToken>());
    }
}
