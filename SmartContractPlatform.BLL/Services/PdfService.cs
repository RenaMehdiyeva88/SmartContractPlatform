using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.BLL.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateContractPdf(Contract contract, string uniqueHash)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generatedAt = DateTime.UtcNow;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Header().Text("Smart Contract").SemiBold().FontSize(20);

                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Text($"Contract Id: {contract.Id}");
                    column.Item().Text($"Template: {contract.Template?.Name ?? contract.TemplateId.ToString()}");
                    column.Item().Text($"Status: {contract.Status}");
                    column.Item().Text($"Created At: {contract.CreatedAt:O}");
                    column.Item().Text($"Generated At: {generatedAt:O}");
                    column.Item().Text($"Unique Hash: {uniqueHash}");

                    column.Item().Text("Parties").SemiBold().FontSize(14);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Name");
                            header.Cell().Element(HeaderCell).Text("FIN/VÖEN");
                            header.Cell().Element(HeaderCell).Text("Role");
                            header.Cell().Element(HeaderCell).Text("Signed");
                            header.Cell().Element(HeaderCell).Text("Signed At");
                        });

                        foreach (var party in contract.Parties)
                        {
                            table.Cell().Element(BodyCell).Text(party.FullNameOrCompany);
                            table.Cell().Element(BodyCell).Text(party.IdentificationNumber);
                            table.Cell().Element(BodyCell).Text(party.Role.ToString());
                            table.Cell().Element(BodyCell).Text(party.Signed ? "Yes" : "No");
                            table.Cell().Element(BodyCell).Text(party.SignedAt?.ToString("O") ?? "-");
                        }
                    });

                    column.Item().Text("Contract Data").SemiBold().FontSize(14);
                    column.Item().Text(contract.DataJson);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container)
        => container.Background(Colors.Grey.Lighten2).Padding(4).Border(1).BorderColor(Colors.Grey.Medium);

    private static IContainer BodyCell(IContainer container)
        => container.Padding(4).Border(1).BorderColor(Colors.Grey.Lighten1);
}