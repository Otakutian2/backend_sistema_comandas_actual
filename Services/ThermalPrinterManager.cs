using iText.Html2pdf;
using iText.Html2pdf.Attach.Impl;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using Microsoft.AspNetCore.Mvc;
using proyecto_backend.Interfaces;
using proyecto_backend.Models;
using proyecto_backend.Utils;
using System.IO;
using System.Text;

namespace proyecto_backend.Services
{
    public class ThermalPrinterManager
    {
        private readonly ICommand _commandService;

        public ThermalPrinterManager(ICommand command)
        {
            _commandService = command;
        }

        public async Task<FileCheckStatus> GeneratePrinterCommand(int commandId, bool showPrice = true)
        {
            FileCheckStatus fileCheckStatus = new FileCheckStatus { status = "error", message = "Error al generar archivo" };

            try
            {
                Command command = await _commandService.GetById(commandId);
                if (command == null) return fileCheckStatus;

                string html = ContentHtml(command, showPrice);

                // CONFIGURACIÓN DE MEDIDAS PARA 80mm
                // 80mm son aproximadamente 226.77 puntos. 
                // Usamos 72mm útiles para evitar recortes físicos en los bordes.
                float widthPoints = 226.77f;

                // El alto puede ser dinámico, pero para tickets largos 500pt a 800pt es seguro.
                // iText cortará la página según el contenido si se configura bien.
                float heightPoints = 800f;

                PageSize pageSize = new PageSize(widthPoints, heightPoints);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    PdfWriter pdfWriter = new PdfWriter(memoryStream);
                    PdfDocument pdfDocument = new PdfDocument(pdfWriter);

                    // Forzamos el tamaño de página sin márgenes internos del PDF
                    pdfDocument.SetDefaultPageSize(pageSize);

                    // IMPORTANTE: Márgenes en 0 porque ya los controlamos en el CSS
                    Document document = new Document(pdfDocument, pageSize);
                    document.SetMargins(0, 0, 0, 0);

                    ConverterProperties properties = new ConverterProperties();
                    properties.SetCharset("UTF-8");

                    HtmlConverter.ConvertToPdf(html, pdfDocument, properties);

                    document.Close();

                    fileCheckStatus.archivo = new FileContentResult(memoryStream.ToArray(), "application/pdf")
                    {
                        FileDownloadName = $"ticket_{command.Id}.pdf"
                    };
                    fileCheckStatus.status = "success";
                }
            }
            catch (Exception ex)
            {
                fileCheckStatus.message = ex.Message;
            }
            return fileCheckStatus;
        }

        public string ContentHtml(Command comanda, bool showPrices)
        {
            StringBuilder builder = new StringBuilder();
            var cultureInfo = new System.Globalization.CultureInfo("es-PE");

            TimeZoneInfo peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            DateTime peruTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruTimeZone);

            string fecha = peruTime.ToString("dd/MM/yyyy");
            string hora = peruTime.ToString("HH:mm");
            int totalProductos = comanda.CommandDetailsCollection.Sum(x => x.DishQuantity);

            builder.Append(@"
    <html>
    <head> 
        <meta charset='UTF-8'>
        <title>Ticket</title>
    ");
            builder.Append(ContentCss());
            builder.Append(@"</head><body>");

            builder.Append(@"<div class='ticket'>");

            // --- ENCABEZADO ---
            string tituloMesa = comanda.TableRestaurantId.HasValue
                ? $"MESA {comanda.TableRestaurantId}"
                : "PARA LLEVAR";

            string nombreCliente = comanda.CustomerAnonymous ?? "CLIENTE";

            builder.Append($@"
        <div class='header-title'>
            <div class='brand'>ORDEN DE PEDIDO</div>
            <div class='big-table'>{tituloMesa}</div>
        </div>
        
        <div class='separator-solid'></div>

        <div class='header-info'>
            <div class='info-row'><span class='lbl'>FECHA:</span> {fecha}</div>
            <div class='info-row'><span class='lbl'>HORA:</span> {hora}</div>
            <div class='info-row'><span class='lbl'>CLIENTE:</span> {nombreCliente.ToUpper()}</div>
            <div class='info-row' style='margin-top:2px'>
                <span class='lbl'>PRODUCTOS:</span> 
                <span style='font-weight:900; font-size:12px'>{totalProductos}</span>
            </div>
        </div>
        
        <div class='separator-dashed'></div>
    ");

            builder.Append("<table class='product-table'><tbody>");

            decimal subTotalCalculado = 0;

            foreach (var item in comanda.CommandDetailsCollection)
            {
                decimal itemPriceTotal = item.DishQuantity * item.DishPrice;
                subTotalCalculado += itemPriceTotal;
                string precioDisplay = showPrices ? itemPriceTotal.ToString("N2", cultureInfo) : "";

                // Producto
                builder.Append($@"
        <tr class='row-product'>
            <td class='col-qty'>{item.DishQuantity}</td>
            <td class='col-name'>{item.Dish.Name.ToUpper()}</td>
            <td class='col-price'>{precioDisplay}</td>
        </tr>");

                // Extras
                if (item.Extras != null && item.Extras.Any())
                {
                    foreach (var extra in item.Extras)
                    {
                        decimal extraTotal = extra.Quantity * (extra.ExtraDish?.Price ?? 0);
                        subTotalCalculado += extraTotal;
                        string precioExtra = (showPrices && extraTotal > 0) ? extraTotal.ToString("N2", cultureInfo) : "";

                        builder.Append($@"
                <tr class='row-extra'>
                    <td></td> 
                    <td class='col-name-extra'>+ {extra.Quantity} {extra.ExtraDish?.Name.ToUpper()}</td>
                    <td class='col-price-extra'>{precioExtra}</td>
                </tr>");
                    }
                }

                // Obs
                if (!string.IsNullOrEmpty(item.Observation))
                {
                    builder.Append($@"
            <tr class='row-obs'>
                <td></td>
                <td colspan='2' class='col-obs'>*** {item.Observation.ToUpper()}</td>
            </tr>");
                }

                builder.Append("<tr><td colspan='3' class='spacer-item'></td></tr>");
            }

            builder.Append("</tbody></table>");

            if (showPrices)
            {
                builder.Append("<div class='separator-solid'></div>");

                builder.Append("<table class='totals-table'>");

                builder.Append($@"
                <tr>
                    <td class='total-lbl'>SUBTOTAL</td>
                    <td class='total-pct'></td> 
                    <td class='total-val'>S/ {subTotalCalculado.ToString("N2", cultureInfo)}</td>
                </tr>");

                decimal montoDescuento = 0;

                if (comanda.Discount > 0)
                {
                    montoDescuento = GlobalUtils.GetDiscountAmount(subTotalCalculado, comanda.Discount, comanda.DiscountType);

                    // Definimos si mostramos el %
                    string porcentajeTexto = "";
                    if (comanda.DiscountType == "percentage")
                    {
                        porcentajeTexto = $"{comanda.Discount.ToString("0")}%";
                    }

                    builder.Append($@"
                    <tr>
                        <td class='total-lbl'>DESCUENTO</td>
                        <td class='total-pct'>{porcentajeTexto}</td>
                        <td class='total-val'>- S/ {montoDescuento.ToString("N2", cultureInfo)}</td>
                    </tr>");
                }

                builder.Append("</table>"); 

                decimal totalFinal = subTotalCalculado - montoDescuento;

                builder.Append($@"
                <div class='final-block'>
                    <div class='final-title'>TOTAL A PAGAR</div>
                    <div class='final-price'>S/ {totalFinal.ToString("N2", cultureInfo)}</div>
                </div>");
            }
            else
            {
                builder.Append("<div class='separator-dashed'></div>");
                builder.Append("<div class='end-mark'>*** FIN DE COMANDA ***</div>");
            }

            // Pie de página simple
            if (showPrices)
            {
                builder.Append(@"
        <div class='footer'>
            ¡Gracias por su preferencia!<br>
            Vuelva pronto
        </div>");
            }

            builder.Append("</div></body></html>");
            return builder.ToString();
        }
        public string ContentCss()
        {
            return @"
    <style>
        @page { margin: 0; }
        body {
            font-family: 'Helvetica', Arial, sans-serif;
            margin: 0;
            padding: 0;
            width: 72mm; 
            color: #000;
        }
        .ticket {
            width: 100%;
            padding: 2px;
            box-sizing: border-box;
        }

        /* --- TAMAÑOS SOLICITADOS --- */
        
        /* Producto Principal: 16px */
        .row-product td { 
            padding-top: 8px; 
            vertical-align: top; 
            font-weight: bold; 
            font-size: 16px; 
            line-height: 1.2;
        }

        /* Extras: 14px */
        .row-extra td { 
            padding-top: 2px; 
            font-weight: normal; 
            font-size: 14px; 
            color: #000;
        }

        /* Observaciones: 13px */
        .col-obs { 
            font-size: 13px; 
            font-style: italic; 
            font-weight: bold; 
            padding-top: 3px;
        }

        /* --- ESTRUCTURA Y CABECERA --- */
        .header-title { text-align: center; margin-bottom: 10px; }
        .brand { font-size: 12px; font-weight: bold; letter-spacing: 1px; }
        .big-table { 
            font-size: 20px; 
            font-weight: 900; 
            text-transform: uppercase; 
            margin-top: 5px; 
            border: 2px solid #000; 
            padding: 5px;
        }

        .header-info { text-align: left; margin: 10px 0; font-size: 13px; }
        .info-row { margin-bottom: 3px; }
        .lbl { font-weight: bold; margin-right: 5px; }

        .separator-solid { border-bottom: 3px solid #000; margin: 8px 0; }
        .separator-dashed { border-bottom: 1px dashed #000; margin: 8px 0; }

        .product-table { width: 100%; border-collapse: collapse; table-layout: fixed; }
        .col-qty { width: 15%; text-align: left; }
        .col-name { width: 55%; text-align: left; }
        .col-price { width: 30%; text-align: right; }

        /* --- TOTALES --- */
        .totals-table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        .total-lbl { width: 50%; text-align: left; font-size: 14px; font-weight: bold; }
        .total-val { width: 50%; text-align: right; font-size: 16px; font-weight: bold; }

        .final-block { text-align: center; margin-top: 15px; border-top: 2px dashed #000; padding-top: 10px; }
        .final-title { font-size: 16px; font-weight: bold; }
        .final-price { font-size: 28px; font-weight: 900; }

        .footer { text-align: center; margin-top: 25px; font-size: 12px; font-weight: bold; }
    </style>";
        }

    }

    public struct FileCheckStatus
    {
        public FileResult archivo { get; set; }
        public string status { get; set; }
        public string message { get; set; }
    }
}