using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rsp.Portal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Services.PdfHandlers;

public static class QuestPdf
{
    public static byte[] GeneratePdf(ProjectModificationRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                page.Content().Column(column =>
                {
                    column.Item().Text("Project Modification Summary")
                        .FontSize(20)
                        .Bold();

                    column.Item().PaddingBottom(10)
                        .LineHorizontal(1);

                    column.Item().Text(text =>
                    {
                        text.Span("Modification ID: ").Bold();
                        text.Span(request.ModificationIdentifier);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Project Record ID: ").Bold();
                        text.Span(request.ProjectRecordId);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Status: ").Bold();
                        text.Span(request.Status);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Modification Type: ").Bold();
                        text.Span(request.ModificationType?.ToString() ?? "N/A");
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Category: ").Bold();
                        text.Span(request.Category?.ToString() ?? "N/A");
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Review Type: ").Bold();
                        text.Span(request.ReviewType?.ToString() ?? "N/A");
                    });

                    column.Item().PaddingTop(20)
                        .Text("Modification Changes")
                        .FontSize(16)
                        .Bold();

                    column.Item().PaddingTop(5)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(200);
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeader).Text("Area of Change");
                                header.Cell().Element(CellHeader).Text("Specific Change");
                            });

                            foreach (var change in request.ProjectModificationChanges)
                            {
                                table.Cell().Element(CellBody)
                                    .Text(change.AreaOfChange ?? "—");

                                table.Cell().Element(CellBody)
                                    .Text(change.SpecificAreaOfChange ?? "—");
                            }
                        });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer CellHeader(IContainer container) =>
        container.Padding(5)
                 .Background(Colors.Grey.Lighten3)
                 .Border(1)
                 .AlignMiddle();

    private static IContainer CellBody(IContainer container) =>
        container.Padding(5)
                 .BorderBottom(1)
                 .BorderColor(Colors.Grey.Lighten2);
}