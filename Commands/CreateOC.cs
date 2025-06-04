using Common;
using MediatR;
using Pd_Ws_Unoee;
using System.Xml.Linq;
using Ts_Ws_Unoee;

namespace Commands
{
    public class CreateOc : IRequestHandler<Rq_Create_Oc, Unit>
    {
        private readonly object _soapClient;

        public CreateOc(object soapClient)
        {
            _soapClient = soapClient;
        }

        public async Task<Unit> Handle(Rq_Create_Oc request, CancellationToken cancellationToken)
        {
            if (_soapClient is Pd_WSUNOEESoapClient pdClient)
            {
                var importRq = new Pd_Ws_Unoee.ImportarXMLRequest(GenerateXml(request), 0);

                var response = await pdClient.ImportarXMLAsync(importRq);

                return ProcessResponse(response, importRq);
            }
            else if (_soapClient is Ts_WSUNOEESoapClient tsClient)
            {
                var importRq = new Ts_Ws_Unoee.ImportarXMLRequest(GenerateXml(request), 0);

                var response = await tsClient.ImportarXMLAsync(importRq);

                return ProcessResponse(response, importRq);
            }
            else
            {
                throw new InvalidOperationException("Tipo de cliente SOAP no soportado.");
            }
        }

        private string GenerateXml(Rq_Create_Oc request)
        {
            var cf = new Common_Functions();
            var xml =
            $@"<?xml version='1.0' encoding='utf-8'?>
            <Importar>
            <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
            <IdCia>1</IdCia>
            <Usuario>{Environment.GetEnvironmentVariable("user")}</Usuario>
            <Clave>{Environment.GetEnvironmentVariable("password")}</Clave>
            <Datos>
            <Linea>000000100000001001</Linea>";

            xml += "<Linea>" +
            cf.plainTextFormat("2", "Num", 7) + // Numero de registro | Numérico | 7 | Si | F_NUMERO-REG | 1 | 7 |  | Numero consecutivo
            cf.plainTextFormat("420", "Num", 4) + // Tipo de registro | Numérico | 4 | Si | F_TIPO-REG | 8 | 11 |  | Valor fijo = 420
            cf.plainTextFormat("0", "Num", 2) + // Subtipo de registro | Numérico | 2 | Si | F_SUBTIPO-REG | 12 | 13 |  | Valor fijo = 00
            cf.plainTextFormat("3", "Num", 2) + // Version del tipo de registro | Numérico | 2 | Si | F_VERSION-REG | 14 | 15 |  | Version = 03
            cf.plainTextFormat("1", "Num", 3) + // Compañía | Numérico | 3 | Si | F_CIA | 16 | 18 |  | Valida en maestro, código de la compañía a la cual pertenece la informacion del registro
            cf.plainTextFormat("1", "Num", 1) + // Indicador para liquidar impuestos | Numérico | 1 | Si | F_LIQUIDA_IMPUESTO | 19 | 19 |  | 0=No liquida impuestos, respeta los que estan en el plano registro tipo 423, 1=Liquida impuestos con los parámetros del sistema (item y proveedor) en el momento de la importación. No tiene en cuenta los registros tipo 423 de este movimiento.
            cf.plainTextFormat("1", "Num", 1) + // Indica si el número consecutivo de docto es manual o automático | Numérico | 1 | Si | F_CONSEC_AUTO_REG | 20 | 20 |  | 0=Manual, significa que respecta el consecutivo asignado en el plano, 1=Automatico, significa que el consecutivo es recalculado con base en la tabla de consecutivos de docto.
            cf.plainTextFormat(request.idCentroOperacion, "Alfa", 3) + // Centro de operación | Alfanumérico | 3 | Si | f420_id_co  | 21 | 23 |  | Valida en maestro, código de centro de operación del documento
            cf.plainTextFormat("OCM", "Alfa", 3) + // Tipo de documento  | Alfanumérico | 3 | Si | f420_id_tipo_docto | 24 | 26 |  | Valida en maestro, código de tipo de documento
            cf.plainTextFormat("0", "Num", 8) + // Consecutivo de documento  | Numérico | 8 | Si | f420_consec_docto | 27 | 34 |  | Numero de documento
            cf.plainTextFormat(request.fecha.ToString("yyyyMMdd"), "Alfa", 8) + // Fecha del documento | Alfanumérico | 8 | Si | f420_fecha | 35 | 42 |  | El plainTextFormato debe ser AAAAMMDD
            cf.plainTextFormat("401", "Num", 3) + // Concepto | Numérico | 3 | Si | f420_id_concepto | 43 | 45 |  | valor fijo: 401
            cf.plainTextFormat("402", "Num", 3) + // Grupo de clase de documento | Numérico | 3 | Si | f420_id_grupo_clase_docto | 46 | 48 |  | Solicitudes 401 y Ordenes de compra 402
            cf.plainTextFormat("404", "Num", 3) + // Clase de documento | Numérico | 3 | Si | f420_id_clase_docto | 49 | 51 |  | Solicitudes 401, Solicitudes valoradas 402, O.C. 404
            cf.plainTextFormat("0", "Num", 1) + // Estado del documento | Numérico | 1 | Si | f420_ind_estado | 52 | 52 |  | 0=En elaboración, 1=Aprobado, 9=Anulado
            cf.plainTextFormat("0", "Num", 1) + // Estado de impresión | Numérico | 1 | Si | f420_ind_impresion | 53 | 53 |  | 0=No, 1=Si
            cf.plainTextFormat("901148822", "Alfa", 15) + // Tercero comprador | Alfanumérico | 15 | Si | f420_id_tercero_sol_comp | 54 | 68 |  | Valida en maestro.  Es el código de tercero con que se identifica el comprador.
            cf.plainTextFormat(request.nitProveedor, "Alfa", 15) + // Tercero proveedor | Alfanumérico | 15 | Dep | f420_id_tercero_prov | 69 | 83 |  | Valida en maestro, código de tercero, oblogatorio si es O.C. y para clase 402.
            cf.plainTextFormat(request.idSucursal, "Alfa", 3) + // Sucursal del proveedor | Alfanumérico | 3 | Dep | f420_id_sucursal_prov | 84 | 86 |  | Valida en maestro, proveedor, oblogatorio si es O.C.
            cf.plainTextFormat("30D", "Alfa", 3) + // Condición de pago | Alfanumérico | 3 | Dep | f420_id_cond_pago | 87 | 89 |  | Valida en maestro, código de condición de pago, oblogatorio si es O.C.  y para clase 402
            cf.plainTextFormat("0", "Num", 1) + // Indicador de tasa | Numérico | 1 | Dep | f420_ind_tasa | 90 | 90 |  | Si el documento es en moneda local coloque 1. Si el documento es en moneda extranjera coloque 1 para que el sistema respete las tasas de cambio reportadas en el plano. SI el documento es en moneda extranjera coloque 0 para que el sistema calcule las tas"
            cf.plainTextFormat(request.idMoneda, "Alfa", 3) + // Moneda del documento | Alfanumérico | 3 | Dep | f420_id_moneda_docto | 91 | 93 |  | Valida en maestro, código de moneda del documento, oblogatorio si es O.C.  y para clase 402
            cf.plainTextFormat("COP", "Alfa", 3) + // Moneda base de conversión | Alfanumérico | 3 | Dep | f420_id_moneda_conv | 94 | 96 |  | Valida en maestro, código de moneda base definida en la compañía, oblogatorio si es O.C.
            cf.plainTextFormat("0", "Num", 13) + // Tasa de conversión | Numérico | 13 | No | f420_tasa_conv | 97 | 109 |  | Si el indicador de tasa es 0 coloque 0. Si el indicador de tasa es 1 coloque 1 si la moneda del documento es igual a la moneda local o a la moneda base de conversion de lo contrario coloque la tasa de conversion. El plainTextFormato debe ser(8 enteros + punto + 4"
            cf.plainTextFormat("COP", "Alfa", 3) + // Moneda local | Alfanumérico | 3 | Dep | f420_id_moneda_local | 110 | 112 |  | Valida en maestro, código de moneda local definida en la compañía. , oblogatorio si es O.C.
            cf.plainTextFormat("0", "Num", 13) + // Tasa local  | Numérico | 13 | Dep | f420_tasa_local | 113 | 125 |  | Si el indicador de tasa es 0 coloque 0. Si el indicador de tasa es 1 coloque 1 si la moneda del documento es igual a la moneda local de lo contrario coloque la tasa de cambio. El plainTextFormato debe ser(8 enteros + punto + 4 decimales) (00000000.0000), oblogato" " +
            cf.plainTextFormat("0", "Num", 8) + // Descuento global 1 | Numérico | 8 | No | f420_tasa_dscto_global1 | 126 | 133 |  | Tasa de descuento global, el plainTextFormato debe ser (3 enteros + punto + 4 decimales) (000.0000)
            cf.plainTextFormat("0", "Num", 8) + // Descuento global 2 | Numérico | 8 | No | f420_tasa_dscto_global2 | 134 | 141 |  | Tasa de descuento global, el plainTextFormato debe ser (3 enteros + punto + 4 decimales) (000.0000)
            cf.plainTextFormat(request.notas, "Alfa", 255) + // Notas | Alfanumérico | 255 | No | f420_notas | 142 | 396 |  | Notas del documento
            cf.plainTextFormat("1", "Num", 1) + // Indicador de contacto | Numérico | 1 | No | F_IND_CONTACTO | 397 | 397 |  | 0=Respeta la información del contacto contenida en el archivo plano. 1 = Al importar el sistema lee el contacto automaticamente del centro de operación.Solo aplica para O.C." " +
            cf.plainTextFormat(" ", "Alfa", 50) + // Contacto | Alfanumérico | 50 | Dep | f419_contacto | 398 | 447 |  | Obligatorio si el indicador de contacto es 0. Nombre de la persona de contacto
            cf.plainTextFormat(" ", "Alfa", 40) + // Direccion 1 | Alfanumérico | 40 | Dep | f419_direccion1 | 448 | 487 |  | Obligatorio si el indicador de contacto es 0. Renglón 1 de la dirección del contacto
            cf.plainTextFormat(" ", "Alfa", 40) + // Direccion 2 | Alfanumérico | 40 | No | f419_direccion2 | 488 | 527 |  | Renglón 2 de la dirección del contacto
            cf.plainTextFormat(" ", "Alfa", 40) + // Direccion 3 | Alfanumérico | 40 | No | f419_direccion3 | 528 | 567 |  | Renglón 3 de la dirección del contacto
            cf.plainTextFormat(" ", "Alfa", 3) + // Pais | Alfanumérico | 3 | No | f419_id_pais | 568 | 570 |  | Valida en maestro, código del país
            cf.plainTextFormat(" ", "Alfa", 2) + // Departamento/Estado | Alfanumérico | 2 | Dep | f419_id_depto | 571 | 572 |  | Obligatorio si el indicador de contacto es 0 y reportan codigo de pais. Valida en maestro, código del departamento
            cf.plainTextFormat(" ", "Alfa", 3) + // Ciudad | Alfanumérico | 3 | Dep | f419_id_ciudad | 573 | 575 |  | Obligatorio si el indicador de contacto es 0 y reportan codigo de Departamento.Valida en maestro, código de la ciudad
            cf.plainTextFormat(" ", "Alfa", 40) + // Barrio | Alfanumérico | 40 | No | f419_id_barrio | 576 | 615 |  | Barrio 
            cf.plainTextFormat(" ", "Alfa", 20) + // Telefono | Alfanumérico | 20 | No | f419_telefono | 616 | 635 |  | Teléfono
            cf.plainTextFormat(" ", "Alfa", 20) + // Fax | Alfanumérico | 20 | No | f419_fax | 636 | 655 |  | Fax
            cf.plainTextFormat(" ", "Alfa", 10) + // Codigo postal | Alfanumérico | 10 | No | f419_cod_postal | 656 | 665 |  | código postal o apartado aéreo
            cf.plainTextFormat(" ", "Alfa", 50) + // E-Mail | Alfanumérico | 50 | No | f419_email | 666 | 715 |  | dirección de correo electrónico
            cf.plainTextFormat(request.doctoReferencia, "Alfa", 15) + // Documento referencia | Alfanumérico | 15 | No | f420_num_docto_referencia | 716 | 730 |  | Documento referencia
            cf.plainTextFormat(" ", "Alfa", 15) + // Mandato | Alfanumérico | 15 | No | f420_id_mandato | 731 | 745 |  | Valida en maestro, código del mandato, solo para ordenes de compra.
            "</Linea>";

            int lineNumber = 2; 
            int registryNumber = 0;

            foreach (var lineaOC in request.lineasOC)
            {
                lineNumber++; 
                registryNumber++;
                xml += "<Linea>" +
                cf.plainTextFormat(lineNumber.ToString(), "Num", 7) + // Numero de registro | Numérico | 7 | Si | F_NUMERO-REG | 1 | 7 | Numero consecutivo
                cf.plainTextFormat("421", "Num", 4) + // Tipo de registro | Numérico | 4 | Si | F_TIPO-REG | 8 | 11 | Valor fijo = 421
                cf.plainTextFormat("", "Num", 2) + // Subtipo de registro | Numérico | 2 | Si | F_SUBTIPO-REG | 12 | 13 | Valor fijo = 00
                cf.plainTextFormat("5", "Num", 2) + // Version del tipo de registro | Numérico | 2 | Si | F_VERSION-REG | 14 | 15 | Version = 08
                cf.plainTextFormat("1", "Num", 3) + // Compañía | Numérico | 3 | Si | F_CIA | 16 | 18 | Valida en maestro, código de la compañía a la cual pertenece la informacion del registro
                cf.plainTextFormat(request.idCentroOperacion, "Alfa", 3) + // Centro de operación | Alfanumérico | 3 | Si | f421_id_co  | 19 | 21 | Valida en maestro, código de centro de operación del documento
                cf.plainTextFormat("OCM", "Alfa", 3) + // Tipo de documento  | Alfanumérico | 3 | Si | f421_id_tipo_docto | 22 | 24 | Valida en maestro, código de tipo de documento
                cf.plainTextFormat("", "Num", 8) + // Consecutivo de documento  | Numérico | 8 | Si | f421_consec_docto | 25 | 32 | Numero de documento
                cf.plainTextFormat(registryNumber.ToString(), "Num", 10) + // Numero de registro | Numérico | 10 | Si | f421_nro_registro | 33 | 42 | Numero de registro del movimiento
                cf.plainTextFormat(" ", "Alfa", 55) + // Campos vacios | Alfanumérico | 55 | No | F_CAMPO | 43 | 97 | campos vacios
                cf.plainTextFormat(lineaOC.idBodega, "Alfa", 5) + // Bodega | Alfanumérico | 5 | Si | f421_id_bodega | 98 | 102 | Valida en maestro, código de bodega
                cf.plainTextFormat("401", "Num", 3) + // Concepto | Numérico | 3 | Si | f421_id_concepto | 103 | 105 | 401
                cf.plainTextFormat(lineaOC.idMotivo, "Alfa", 2) + // Motivo | Alfanumérico | 2 | Si | f421_id_motivo | 106 | 107 | Valida en maestro, código de motivo
                cf.plainTextFormat("", "Num", 1) + // Indicador de obsequio | Numérico | 1 | Si | f421_ind_obsequio | 108 | 108 | Indicador de obsequio 0=No, 1=Si
                cf.plainTextFormat(request.idCentroOperacion, "Alfa", 3) + // Centro de operación movimiento | Alfanumérico | 3 | Dep | f421_id_co_movto | 109 | 111 | Obligatorio si no es obsequio. Valida en maestro, código de centro de operación del movimiento
                cf.plainTextFormat(" ", "Alfa", 2) + // Campos vacios | Alfanumérico | 2 | No | F_CAMPO | 112 | 113 | campos vacios
                cf.plainTextFormat(" ", "Alfa", 15) + // Centro de costo movimiento | Alfanumérico | 15 | Dep | f421_id_ccosto_movto | 114 | 128 | Obligatorio si no es obsequio y es un servicio cuya cuenta contable exige ccosto. Valida en maestro, código de centro de costo del movimiento.
                cf.plainTextFormat(lineaOC.idProyecto, "Alfa", 15) + // Proyecto | Alfanumérico | 15 | No | f421_id_proyecto | 129 | 143 | Valida en maestro, código de proyecto del movimiento
                cf.plainTextFormat(lineaOC.idUMedida, "Alfa", 4) + // Unidad de medida | Alfanumérico | 4 | Si | f421_id_unidad_medida | 144 | 147 | Valida en maestro, código de unidad de medida del movimiento
                cf.plainTextFormat(lineaOC.cantidad.ToString(), "Num", 15) + ".0000" + // Cantidad pedida  | Numérico | 20 | Si | f421_cant_pedida_base | 148 | 167 | Cantidad pedida base, el plainTextFormato debe ser (15 enteros + punto + 4 decimales). El número de decimales se deben reportar teniendo en cuenta el número de decimales configurados en la unidad de medidad. (000000000000000.0000)"
                cf.plainTextFormat(lineaOC.fechaEntrega.ToString("yyyyMMdd"), "Alfa", 8) + // Fecha de entrega | Alfanumérico | 8 | Si | f421_fecha_entrega | 168 | 175 | El plainTextFormato debe ser AAAAMMDD
                cf.plainTextFormat(" ", "Alfa", 15) + // Item del proveedor | Alfanumérico | 15 | No | f421_cod_item_prov | 176 | 190 | No se valida con maestros
                //cf.plainTextFormat(lineaOC.UnitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture), "Float", 15, 4) + // Precio unitario / Precio sugerido en solicitudes de compra | Numérico | 20 | Dep | f421_precio_unitario | 191 | 210 | Debe ser mayor a 0 si no es obsequio. El plainTextFormato debe ser (15 enteros + punto + 4 decimales). El número de decimales se deben reportar teniendo en cuenta el número de decimales configurados en la moneda (decimales unidades).Aplica para clase 401, 402 y 404. Solo para versión 3 de documento: Puede ser cero si se liquidan impuestos con los parametros del sistema  (item y proveedor), se tomará el precio del sistema.
                cf.plainTextFormat("000000000000000.0000", "Alfa", 20) + // Precio unitario / Precio sugerido en solicitudes de compra | Numérico | 20 | Dep | f421_precio_unitario | 191 | 210 | Debe ser mayor a 0 si no es obsequio. El formato debe ser (15 enteros + punto + 4 decimales). El número de decimales se deben reportar teniendo en cuenta el número de decimales configurados en la moneda (decimales unidades).Aplica para clase 401, 402 y 404. Solo para versión 3 de documento: Puede ser cero si se liquidan impuestos con los parametros del sistema  (item y proveedor), se tomará el precio del sistema.
                cf.plainTextFormat(lineaOC.notas, "Alfa", 255) + // Notas | Alfanumérico | 255 | No | f421_notas | 211 | 465 | Notas del movimiento
                cf.plainTextFormat(lineaOC.detalle, "Alfa", 2000) + // Detalle | Alfanumérico | 2000 | No | f421_detalle | 466 | 2465 | Detalle del item
                cf.plainTextFormat(" ", "Alfa", 40) + // Descripción del item | Alfanumérico | 40 | No | F_DESC_ITEM | 2466 | 2505 | Si tiene algún valor  el sistema al importar el registro valida que la descripción del item sea idem al de la base de datos. 
                cf.plainTextFormat(" ", "Alfa", 4) + // Unidad de medida de inventario del item. | Alfanumérico | 4 | No | F_ID_UM_INVENTARIO | 2506 | 2509 | Si tiene algún valor  el sistema al importar el registro valida que la unidad de inventario del item sea idem al de la base de datos. 
                cf.plainTextFormat("", "Num", 7) + // Item | Numérico | 7 | Dep | f421_id_item | 2510 | 2516 | Codigo, es obligatorio si no va referencia ni codigo de barras
                cf.plainTextFormat(lineaOC.referenciaItem, "Alfa", 50) + // Referencia item | Alfanumérico | 50 | Dep | f421_referencia_item | 2517 | 2566 | Codigo, es obligatorio si no va codigo de item ni codigo de barras
                cf.plainTextFormat(" ", "Alfa", 20) + // Codigo de barras | Alfanumérico | 20 | Dep | f421_codigo_barras | 2567 | 2586 | Codigo, es obligatorio si no va codigo de item ni referencia
                cf.plainTextFormat(" ", "Alfa", 20) + // Extension 1 | Alfanumérico | 20 | Dep | f421_id_ext1_detalle | 2587 | 2606 | Es obligatorio si el ítem maneja extensión 1
                cf.plainTextFormat(" ", "Alfa", 20) + // Extension 2 | Alfanumérico | 20 | Dep | f421_id_ext2_detalle | 2607 | 2626 | Es obligatorio si el ítem maneja extensión 2
                cf.plainTextFormat(lineaOC.idUnidadNegocio, "Alfa", 20) + // Unidad de negocio movimiento | Alfanumérico | 20 | Dep | f421_id_un_movto | 2627 | 2646 | Obligatorio si no es obsequio. Valida en maestro, código de unidad de negocio del movimiento.
                cf.plainTextFormat("", "Num", 8) + // Tasa de descuento condicionado | Numérico | 8 | No | f421_tasa_dscto_condicionado | 2647 | 2654 | Tasa del descuento condicionado. El plainTextFormato debe ser (3 enteros + punto + 4 decimales) (000.0000).  Aplica solo para ordenes de compra.
                cf.plainTextFormat(" ", "Alfa", 3) + // Tipo de documento  | Alfanumérico | 3 | No | f850_id_tipo_docto_op | 2655 | 2657 | Valida en maestro, código de tipo de documento
                cf.plainTextFormat("", "Num", 8) + // Consecutivo de documento  | Numérico | 8 | No | f850_consec_docto_op | 2658 | 2665 | Numero de documento
                cf.plainTextFormat("", "Num", 7) + // Item | Numérico | 7 | Dep | f865_id_item_op | 2666 | 2672 | Codigo, es obligatorio si no va referencia ni codigo de barras, y si numero de documento de orden de producción  existe
                cf.plainTextFormat(" ", "Alfa", 50) + // Referencia item | Alfanumérico | 50 | Dep | f865_referencia_item_op | 2673 | 2722 | Codigo, es obligatorio si no va codigo de item ni codigo de barras, y si numero de documento de orden de producción existe
                cf.plainTextFormat(" ", "Alfa", 20) + // Codigo de barras | Alfanumérico | 20 | Dep | f865_codigo_barras_item_op | 2723 | 2742 | Codigo, es obligatorio si no va codigo de item ni referencia, y si numero de documento orden de producción existe.
                cf.plainTextFormat(" ", "Alfa", 20) + // Extension 1 | Alfanumérico | 20 | Dep | f851_id_ext1_detalle_item_op | 2743 | 2762 | Es obligatorio si el ítem maneja extensión 1, y numero de documento de  orden de producción existe
                cf.plainTextFormat(" ", "Alfa", 20) + // Extension 2 | Alfanumérico | 20 | Dep | f851_id_ext2_detalle_item_op | 2763 | 2782 | Es obligatorio si el ítem maneja extensión 2, y numero de documento de orden de producción  existe
                cf.plainTextFormat("", "Num", 10) + // Número de operación | Numérico | 10 | Dep | f865_numero_operacion | 2783 | 2792 | Valida en maestro, número de la operación, y es obligatorio, si existe numero de documento de orden de producción.
                "</Linea>";
            }

            lineNumber++;
            xml += "<Linea>" + cf.plainTextFormat(lineNumber.ToString(), "Num", 7) + "99990001001</Linea></Datos></Importar>";

            return xml;
        }

        private Unit ProcessResponse(dynamic response, object importRq)
        {
            if (response.printTipoError == 0)
            {
                return Unit.Value;
            }
            else
            {
                string rawResponse = response?.ImportarXMLResult?.Nodes?[1]?.FirstNode?.ToString();

                XDocument xmlDoc = XDocument.Parse(rawResponse);

                var errores = xmlDoc.Descendants("Table")
                    .Select(table => new
                    {
                        Linea = table.Element("f_nro_linea")?.Value,
                        Valor = table.Element("f_valor")?.Value,
                        Detalle = table.Element("f_detalle")?.Value
                    })
                    .Where(e => !string.IsNullOrEmpty(e.Linea) && !string.IsNullOrEmpty(e.Detalle))
                    .Select(e => $"Línea: {e.Linea}, Valor: {e.Valor}, Detalle: {e.Detalle}")
                    .ToList();

                string mensajeError = string.Join("\n", errores);

                throw new ValidationException($"Error en la importación:\n{mensajeError}");
            }
        }
    }

    public class Rq_Create_Oc: IRequest<Unit>
    {
        public string idCentroOperacion { get; set; }
        public DateTime fecha { get; set; }
        public string nitProveedor { get; set; }
        public string idSucursal { get; set; }
        public string idMoneda { get; set; }
        public string notas { get; set; }
        public string doctoReferencia { get; set; }
        public List<LineaOc> lineasOC { get; set; }
    }

    public class LineaOc {
        public string idBodega { get; set; }
        public string idMotivo { get; set; }
        public string idProyecto { get; set; }
        public string idUMedida { get; set; }
        public float cantidad { get; set; }
        public DateTime fechaEntrega { get; set; }
        public float precioUnitario { get; set; }
        public string notas { get; set; }
        public string detalle { get; set; }
        public string referenciaItem { get; set; }
        public string idUnidadNegocio { get; set; }
    }
}
