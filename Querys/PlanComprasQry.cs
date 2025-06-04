using Ts_Ws_Unoee;
using Pd_Ws_Unoee;
using MediatR;
using System.Xml.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Querys
{
    public class PlanComprasQry : IRequestHandler<Rq_Plan_Compras, List<Linea_Plan_Compras>>
    {
        private readonly object _soapClient;
        private readonly IDbConnection _sqlServerConnection;
        private readonly ILogger<PlanComprasQry> _logger;

        public PlanComprasQry(object soapClient, SqlConnection sqlServerConnection, ILogger<PlanComprasQry> logger)
        {
            _soapClient = soapClient;
            _sqlServerConnection = sqlServerConnection;
            _logger = logger;
        }

        public async Task<List<Linea_Plan_Compras>> Handle(Rq_Plan_Compras request, CancellationToken cancellationToken)
        {
            if (request.tipoConsulta == "ws")
            {
                var xmlQuery =
                $@"<Consulta>
                <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
                <IdCia>1</IdCia>
                <IdProveedor>Integraciones</IdProveedor>";

                switch (request.calcularPor)
                {
                    case "undOrden": xmlQuery += "<IdConsulta>PlanComprasUMorden</IdConsulta>"; break;
                    case "undEmpaque": xmlQuery += "<IdConsulta>PlanComprasUMempaque</IdConsulta>"; break;
                    case "undInventario": xmlQuery += "<IdConsulta>PlanComprasUMinventario</IdConsulta>"; break;
                    default: throw new InvalidOperationException("Calcular por no soportado.");
                }

                xmlQuery +=
                $@"<Usuario>{Environment.GetEnvironmentVariable("user")}</Usuario>
                <Clave>{Environment.GetEnvironmentVariable("password")}</Clave>
                <Parametros>
                    <idsEmpresas>{request.idsEmpresas}</idsEmpresas>
                    <idsUnidadesNegocio>{request.idsUnidadesNegocio}</idsUnidadesNegocio>
                    <idCriterioProveedor>{request.idCriterioProveedor}</idCriterioProveedor>
                    <idCriterioLinea>{request.idCriterioLinea}</idCriterioLinea>
                </Parametros>
                </Consulta>";

                if (_soapClient is Pd_WSUNOEESoapClient pdClient)
                {
                    var result = await pdClient.EjecutarConsultaXMLAsync(xmlQuery);

                    return data(result.Nodes[1].FirstNode.ToString());
                }
                else if (_soapClient is Ts_WSUNOEESoapClient tsClient)
                {
                    var result = await tsClient.EjecutarConsultaXMLAsync(xmlQuery);

                    return data(result.Nodes[1].FirstNode.ToString());
                }
                else
                {
                    throw new InvalidOperationException("Tipo de cliente SOAP no soportado.");
                }
            }
            else {

				string sqlQuery = "";

                switch (request.calcularPor)
                {
                    case "undOrden":
                        sqlQuery = @$"
						select
							rtrim(Item.v121_referencia) as referenciaItem,
							rtrim(case Item.v121_notas_item when '' then Item.v121_descripcion else Item.v121_notas_item end) as descripcionItem,
							rtrim(Item.v121_id_unidad_inventario) as uMedidaInventario,
							rtrim(Item.v121_id_unidad_orden) as uMedidaOrden,
							rtrim(isnull(Item.v121_id_unidad_empaque,' ')) as uMedidaEmpaque,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(getdate())-1),getdate()),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,getdate()))),dateadd(mm,1,getdate())),23) ) ) )
								)
							,0)) as unMes,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-1,getdate()))-1),dateadd(month,-1,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-1,getdate())))),dateadd(mm,1,dateadd(month,-1,getdate()))),23) ) ) )
								)
							,0)) as dosMeses,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-2,getdate()))-1),dateadd(month,-2,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-2,getdate())))),dateadd(mm,1,dateadd(month,-2,getdate()))),23) ) ) )
								)
							,0)) as tresMeses,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-3,getdate()))-1),dateadd(month,-3,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-3,getdate())))),dateadd(mm,1,dateadd(month,-3,getdate()))),23) ) ) )
								)
							,0)) as cuatroMeses,
							convert(numeric(18,2),isnull(
								(select
									sum(dbo.t431_cm_pv_movto.f431_cant1_comprometida / t122_mc_items_unidades.f122_factor)
								from     
									dbo.t430_cm_pv_docto inner join
									dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
									v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								where  
									(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as rem,
							convert(numeric(18,2),isnull(
								(select
									sum((dbo.t431_cm_pv_movto.f431_cant1_pedida - dbo.t431_cm_pv_movto.f431_cant1_comprometida - dbo.t431_cm_pv_movto.f431_cant1_remisionada) / t122_mc_items_unidades.f122_factor)
								from
									dbo.t430_cm_pv_docto inner join
									dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
									v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								where  
									(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t431_cm_pv_movto.f431_ind_estado = 0 OR dbo.t431_cm_pv_movto.f431_ind_estado = 1 OR dbo.t431_cm_pv_movto.f431_ind_estado = 2 OR dbo.t431_cm_pv_movto.f431_ind_estado = 3) AND
									(dbo.t430_cm_pv_docto.f430_id_clase_docto = 502) AND
									(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0))	as pedidoAct,
							isnull(
								(select STUFF(
									(select ', ' 
										+ case dbo.t431_cm_pv_movto.f431_ind_estado when 0 then 'En Elaboracion: ' when 1 then 'Retenido: ' when 2 then 'Aprobado: ' when 3 then 'Comprometido: ' when 9 then 'Anulado: ' else '?: ' end 
										+ convert(varchar, convert(numeric(18,2),sum((dbo.t431_cm_pv_movto.f431_cant1_pedida - dbo.t431_cm_pv_movto.f431_cant1_comprometida - dbo.t431_cm_pv_movto.f431_cant1_remisionada) / t122_mc_items_unidades.f122_factor) ) )
									 from
										dbo.t430_cm_pv_docto inner join
										dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
										v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
										t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
										t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
									 where 
										(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
										(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
										(dbo.t431_cm_pv_movto.f431_ind_estado = 0 OR dbo.t431_cm_pv_movto.f431_ind_estado = 1 OR dbo.t431_cm_pv_movto.f431_ind_estado = 2 OR dbo.t431_cm_pv_movto.f431_ind_estado = 3) AND
										(dbo.t430_cm_pv_docto.f430_id_clase_docto = 502) AND
										(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
									group by dbo.t431_cm_pv_movto.f431_ind_estado
									FOR XML PATH('') )
								, 1, 2, '') ) , '') as cantidadesPedidoActPorEstado,
							convert(numeric(18,2),isnull(
								(select   sum 
									((t400_cm_existencia.f400_cant_existencia_1 - t400_cm_existencia.f400_cant_comprometida_1) / t122_mc_items_unidades.f122_factor)
								 from            
									t400_cm_existencia inner join
									v121 on t400_cm_existencia.f400_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								 where        
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(f400_rowid_bodega not in (3,4,7,8)) AND
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t122_mc_items_unidades.f122_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t120_mc_items.f120_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t400_cm_existencia.f400_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as cantDisponible,
							convert(numeric(18,2),isnull(
								(select   sum    
									(f400_cant_pendiente_entrar_1 / t122_mc_items_unidades.f122_factor)
								 from            
									t400_cm_existencia inner join
									v121 on t400_cm_existencia.f400_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_orden = t122_mc_items_unidades.f122_id_unidad
								 where        
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(f400_rowid_bodega not in (3,4,7,8)) AND
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t122_mc_items_unidades.f122_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t120_mc_items.f120_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t400_cm_existencia.f400_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as compraPdte
						from 
							dbo.v121 as Item
						where
							(select f125_id_criterio_mayor from dbo.t125_mc_items_criterios CriterioProveedor where CriterioProveedor.f125_id_plan = '40' and CriterioProveedor.f125_rowid_item = Item.v121_rowid_item ) = @idCriterioProveedor AND
							(select f125_id_criterio_mayor from dbo.t125_mc_items_criterios CriterioLinea where CriterioLinea.f125_id_plan = '10' and CriterioLinea.f125_rowid_item = Item.v121_rowid_item ) = @idCriterioLinea AND
							Item.v121_id_cia = 1
						order by descripcionItem";
                        break;
                    case "undEmpaque":
						sqlQuery = @$"
						select
							rtrim(Item.v121_referencia) as referenciaItem,
							rtrim(case Item.v121_notas_item when '' then Item.v121_descripcion else Item.v121_notas_item end) as descripcionItem,
							rtrim(Item.v121_id_unidad_inventario) as uMedidaInventario,
							rtrim(Item.v121_id_unidad_orden) as uMedidaOrden,
							rtrim(isnull(Item.v121_id_unidad_empaque,' ')) as uMedidaEmpaque,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(getdate())-1),getdate()),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,getdate()))),dateadd(mm,1,getdate())),23) ) ) )
								)
							,0)) as unMes,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-1,getdate()))-1),dateadd(month,-1,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-1,getdate())))),dateadd(mm,1,dateadd(month,-1,getdate()))),23) ) ) )
								)
							,0)) as dosMeses,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-2,getdate()))-1),dateadd(month,-2,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-2,getdate())))),dateadd(mm,1,dateadd(month,-2,getdate()))),23) ) ) )
								)
							,0)) as tresMeses,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end / f122_factor ) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-3,getdate()))-1),dateadd(month,-3,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-3,getdate())))),dateadd(mm,1,dateadd(month,-3,getdate()))),23) ) ) )
								)
							,0)) as cuatroMeses,
							convert(numeric(18,2),isnull(
								(select
									sum(dbo.t431_cm_pv_movto.f431_cant1_comprometida / t122_mc_items_unidades.f122_factor)
								from     
									dbo.t430_cm_pv_docto inner join
									dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
									v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								where  
									(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as rem,
							convert(numeric(18,2),isnull(
								(select
									sum((dbo.t431_cm_pv_movto.f431_cant1_pedida - dbo.t431_cm_pv_movto.f431_cant1_comprometida - dbo.t431_cm_pv_movto.f431_cant1_remisionada) / t122_mc_items_unidades.f122_factor)
								from
									dbo.t430_cm_pv_docto inner join
									dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
									v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								where  
									(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t431_cm_pv_movto.f431_ind_estado = 0 OR dbo.t431_cm_pv_movto.f431_ind_estado = 1 OR dbo.t431_cm_pv_movto.f431_ind_estado = 2 OR dbo.t431_cm_pv_movto.f431_ind_estado = 3) AND
									(dbo.t430_cm_pv_docto.f430_id_clase_docto = 502) AND
									(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0))	as pedidoAct,
							isnull(
								(select STUFF(
									(select ', ' 
										+ case dbo.t431_cm_pv_movto.f431_ind_estado when 0 then 'En Elaboracion: ' when 1 then 'Retenido: ' when 2 then 'Aprobado: ' when 3 then 'Comprometido: ' when 9 then 'Anulado: ' else '?: ' end 
										+ convert(varchar, convert(numeric(18,2),sum((dbo.t431_cm_pv_movto.f431_cant1_pedida - dbo.t431_cm_pv_movto.f431_cant1_comprometida - dbo.t431_cm_pv_movto.f431_cant1_remisionada) / t122_mc_items_unidades.f122_factor) ) )
									 from
										dbo.t430_cm_pv_docto inner join
										dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
										v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
										t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
										t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
									 where 
										(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
										(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
										(dbo.t431_cm_pv_movto.f431_ind_estado = 0 OR dbo.t431_cm_pv_movto.f431_ind_estado = 1 OR dbo.t431_cm_pv_movto.f431_ind_estado = 2 OR dbo.t431_cm_pv_movto.f431_ind_estado = 3) AND
										(dbo.t430_cm_pv_docto.f430_id_clase_docto = 502) AND
										(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
									group by dbo.t431_cm_pv_movto.f431_ind_estado
									FOR XML PATH('') )
								, 1, 2, '') ) , '') as cantidadesPedidoActPorEstado,
							convert(numeric(18,2),isnull(
								(select   sum 
									((t400_cm_existencia.f400_cant_existencia_1 - t400_cm_existencia.f400_cant_comprometida_1) / t122_mc_items_unidades.f122_factor)
								 from            
									t400_cm_existencia inner join
									v121 on t400_cm_existencia.f400_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								 where        
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(f400_rowid_bodega not in (3,4,7,8)) AND
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t122_mc_items_unidades.f122_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t120_mc_items.f120_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t400_cm_existencia.f400_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as cantDisponible,
							convert(numeric(18,2),isnull(
								(select   sum    
									(f400_cant_pendiente_entrar_1 / t122_mc_items_unidades.f122_factor)
								 from            
									t400_cm_existencia inner join
									v121 on t400_cm_existencia.f400_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid inner join
									t122_mc_items_unidades on t120_mc_items.f120_rowid = t122_mc_items_unidades.f122_rowid_item AND v121.v121_id_unidad_empaque = t122_mc_items_unidades.f122_id_unidad
								 where        
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(f400_rowid_bodega not in (3,4,7,8)) AND
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t122_mc_items_unidades.f122_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t120_mc_items.f120_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t400_cm_existencia.f400_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as compraPdte
						from 
							dbo.v121 as Item
						where
							(select f125_id_criterio_mayor from dbo.t125_mc_items_criterios CriterioProveedor where CriterioProveedor.f125_id_plan = '40' and CriterioProveedor.f125_rowid_item = Item.v121_rowid_item ) = @idCriterioProveedor AND
							(select f125_id_criterio_mayor from dbo.t125_mc_items_criterios CriterioLinea where CriterioLinea.f125_id_plan = '10' and CriterioLinea.f125_rowid_item = Item.v121_rowid_item ) = @idCriterioLinea AND
							Item.v121_id_cia = 1
						order by descripcionItem";
						break;
                    case "undInventario":
						sqlQuery = @$"
						select
							rtrim(Item.v121_referencia) as referenciaItem,
							rtrim(case Item.v121_notas_item when '' then Item.v121_descripcion else Item.v121_notas_item end) as descripcionItem,
							rtrim(Item.v121_id_unidad_inventario) as uMedidaInventario,
							rtrim(Item.v121_id_unidad_orden) as uMedidaOrden,
							rtrim(isnull(Item.v121_id_unidad_empaque,' ')) as uMedidaEmpaque,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(getdate())-1),getdate()),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,getdate()))),dateadd(mm,1,getdate())),23) ) ) )
								)
							,0)) as unMes,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-1,getdate()))-1),dateadd(month,-1,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-1,getdate())))),dateadd(mm,1,dateadd(month,-1,getdate()))),23) ) ) )
								)
							,0)) as dosMeses,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-2,getdate()))-1),dateadd(month,-2,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-2,getdate())))),dateadd(mm,1,dateadd(month,-2,getdate()))),23) ) ) )
								)
							,0)) as tresMeses,
							convert(numeric(18,2),isnull(
								(select 
									sum(case f461_id_clase_docto when 522 then f470_cant_1 when 523 then f470_cant_1 when 525 then f470_cant_1*(-1)	when 526 then f470_cant_1*(-1) end) as f470_vlr_bruto
								from     
									dbo.t350_co_docto_contable inner join
									dbo.t461_cm_docto_factura_venta on dbo.t350_co_docto_contable.f350_rowid = dbo.t461_cm_docto_factura_venta.f461_rowid_docto inner join
									dbo.t470_cm_movto_invent on dbo.t461_cm_docto_factura_venta.f461_rowid_docto = dbo.t470_cm_movto_invent.f470_rowid_docto_fact inner join
									dbo.v121 on dbo.v121.v121_rowid_item_ext = dbo.t470_cm_movto_invent.f470_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								where
									(dbo.t350_co_docto_contable.f350_ind_estado = 1) AND 
									(dbo.t350_co_docto_contable.f350_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t461_cm_docto_factura_venta.f461_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t470_cm_movto_invent.f470_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.v121.v121_referencia = Item.v121_referencia) AND
									(dbo.t350_co_docto_contable.f350_fecha >= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(month,-3,getdate()))-1),dateadd(month,-3,getdate())),23) ) ) ) AND
									(dbo.t350_co_docto_contable.f350_fecha <= convert(date, ( convert(varchar(25),dateadd(dd,-(day(dateadd(mm,1,dateadd(month,-3,getdate())))),dateadd(mm,1,dateadd(month,-3,getdate()))),23) ) ) )
								)
							,0)) as cuatroMeses,
							convert(numeric(18,2),isnull(
								(select
									sum(dbo.t431_cm_pv_movto.f431_cant1_comprometida)
								from     
									dbo.t430_cm_pv_docto inner join
									dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
									v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								where  
									(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as rem,
							convert(numeric(18,2),isnull(
								(select
									sum(dbo.t431_cm_pv_movto.f431_cant1_pedida - dbo.t431_cm_pv_movto.f431_cant1_comprometida - dbo.t431_cm_pv_movto.f431_cant1_remisionada)
								from
									dbo.t430_cm_pv_docto inner join
									dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
									v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								where  
									(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t431_cm_pv_movto.f431_ind_estado = 0 OR dbo.t431_cm_pv_movto.f431_ind_estado = 1 OR dbo.t431_cm_pv_movto.f431_ind_estado = 2 OR dbo.t431_cm_pv_movto.f431_ind_estado = 3) AND
									(dbo.t430_cm_pv_docto.f430_id_clase_docto = 502) AND
									(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0))	as pedidoAct,
							isnull(
								(select STUFF(
									(select ', ' 
										+ case dbo.t431_cm_pv_movto.f431_ind_estado when 0 then 'En Elaboracion: ' when 1 then 'Retenido: ' when 2 then 'Aprobado: ' when 3 then 'Comprometido: ' when 9 then 'Anulado: ' else '?: ' end 
										+ convert(varchar, convert(numeric(18,2),sum(dbo.t431_cm_pv_movto.f431_cant1_pedida - dbo.t431_cm_pv_movto.f431_cant1_comprometida - dbo.t431_cm_pv_movto.f431_cant1_remisionada) ) )
									 from
										dbo.t430_cm_pv_docto inner join
										dbo.t431_cm_pv_movto on dbo.t430_cm_pv_docto.f430_rowid = dbo.t431_cm_pv_movto.f431_rowid_pv_docto inner join
										v121 on f431_rowid_item_ext = v121.v121_rowid_item_ext inner join
										t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
									 where 
										(dbo.t430_cm_pv_docto.f430_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
										(dbo.t431_cm_pv_movto.f431_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
										(dbo.t431_cm_pv_movto.f431_ind_estado = 0 OR dbo.t431_cm_pv_movto.f431_ind_estado = 1 OR dbo.t431_cm_pv_movto.f431_ind_estado = 2 OR dbo.t431_cm_pv_movto.f431_ind_estado = 3) AND
										(dbo.t430_cm_pv_docto.f430_id_clase_docto = 502) AND
										(dbo.t431_cm_pv_movto.f431_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
									group by dbo.t431_cm_pv_movto.f431_ind_estado
									FOR XML PATH('') )
								, 1, 2, '') ) , '') as cantidadesPedidoActPorEstado,
							convert(numeric(18,2),isnull(
								(select   sum 
									(t400_cm_existencia.f400_cant_existencia_1 - t400_cm_existencia.f400_cant_comprometida_1)
								 from            
									t400_cm_existencia inner join
									v121 on t400_cm_existencia.f400_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								 where        
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(f400_rowid_bodega not in (3,4,7,8)) AND
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t120_mc_items.f120_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t400_cm_existencia.f400_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as cantDisponible,
							convert(numeric(18,2),isnull(
								(select   sum    
									(f400_cant_pendiente_entrar_1)
								 from            
									t400_cm_existencia inner join
									v121 on t400_cm_existencia.f400_rowid_item_ext = v121.v121_rowid_item_ext inner join
									t120_mc_items on v121.v121_rowid_item = t120_mc_items.f120_rowid
								 where        
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(f400_rowid_bodega not in (3,4,7,8)) AND
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t120_mc_items.f120_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(v121.v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND 
									(t400_cm_existencia.f400_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))) AND
									(dbo.t400_cm_existencia.f400_rowid_item_ext in (select v121_rowid_item_ext from v121 where v121_referencia = Item.v121_referencia and v121_id_cia in (select * from wf_Split_Cadena(@idsEmpresas,','))))
								)
							,0)) as compraPdte
						from 
							dbo.v121 as Item
						where
							(select f125_id_criterio_mayor from dbo.t125_mc_items_criterios CriterioProveedor where CriterioProveedor.f125_id_plan = '40' and CriterioProveedor.f125_rowid_item = Item.v121_rowid_item ) = @idCriterioProveedor AND
							(select f125_id_criterio_mayor from dbo.t125_mc_items_criterios CriterioLinea where CriterioLinea.f125_id_plan = '10' and CriterioLinea.f125_rowid_item = Item.v121_rowid_item ) = @idCriterioLinea AND
							Item.v121_id_cia = 1
						order by descripcionItem";
						break;
                    default: throw new InvalidOperationException("Calcular por no soportado.");
                }                

                var rows = await _sqlServerConnection.QueryAsync<Linea_Plan_Compras>(sqlQuery,  request);

				return rows.ToList();

            }      
        }

        private List<Linea_Plan_Compras> data(string result)
        {
            var dataSet = XElement.Parse(result);

            var lineasPlanCompras = new List<Linea_Plan_Compras>();

            foreach (XElement xe in dataSet.Elements())
            {
                lineasPlanCompras.Add(
                    new Linea_Plan_Compras
                    {
                        referenciaItem = xe.Element("referenciaItem").Value,
                        descripcionItem = xe.Element("descripcionItem").Value,
                        uMedidaInventario = xe.Element("uMedidaInventario").Value,
                        uMedidaEmpaque = xe.Element("uMedidaEmpaque").Value,
                        uMedidaOrden = xe.Element("uMedidaOrden").Value,
                        unMes = (decimal)xe.Element("unMes"),
                        dosMeses = (decimal)xe.Element("dosMeses"),
                        tresMeses = (decimal)xe.Element("tresMeses"),
                        cuatroMeses = (decimal)xe.Element("cuatroMeses"),
                        rem = (decimal)xe.Element("rem"),
                        pedidoAct = (decimal)xe.Element("pedidoAct"),
                        cantidadesPedidoActPorEstado = (string)xe.Element("cantidadesPedidoActPorEstado"),
                        cantDisponible = (decimal)xe.Element("cantDisponible"),
                        compraPdte = (decimal)xe.Element("compraPdte"),
                    }
                );
            }

            return lineasPlanCompras;
        }

    }

    public class Rq_Plan_Compras : IRequest<List<Linea_Plan_Compras>> {
        public string idsEmpresas { get; set; }
        public string idsUnidadesNegocio { get; set; }
        public string idCriterioProveedor { get; set; }
        public string idCriterioLinea { get; set; }
        public string calcularPor { get; set; }
        public string? tipoConsulta { get; set; }
    }

    public class Linea_Plan_Compras {
        public string referenciaItem { get; set; }
        public string descripcionItem { get; set; }
        public string uMedidaInventario { get; set; }
        public string uMedidaOrden { get; set; }
        public string uMedidaEmpaque { get; set; }
        public decimal unMes { get; set; }
        public decimal dosMeses { get; set; }
        public decimal tresMeses { get; set; }
        public decimal cuatroMeses { get; set; }
        public decimal rem { get; set; }
        public decimal pedidoAct { get; set; }
        public string cantidadesPedidoActPorEstado { get; set; }
        public decimal cantDisponible { get; set; }
        public decimal compraPdte { get; set; }
    }
}
