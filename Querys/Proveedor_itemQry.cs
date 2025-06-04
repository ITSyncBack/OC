using MediatR;
using System.Xml.Linq;
using Ts_Ws_Unoee;
using Pd_Ws_Unoee;

namespace Querys
{
    public class Proveedor_ItemQry : IRequestHandler<Rq_Proveedor_Item, Proveedor_Item>
    {
        private readonly object _soapClient;

        public Proveedor_ItemQry(object soapClient)
        {
            _soapClient = soapClient;
        }
        public async Task<Proveedor_Item> Handle(Rq_Proveedor_Item request, CancellationToken cancellationToken)
        {
            var xmlQuery =
            $@"<?xml version='1.0' encoding='utf-8'?>
            <Consulta>
            <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
            <IdCia>1</IdCia>
            <IdProveedor>Integraciones</IdProveedor>
            <IdConsulta>ProveedorItem</IdConsulta>
            <Usuario>{Environment.GetEnvironmentVariable("user")}</Usuario>
            <Clave>{Environment.GetEnvironmentVariable("password")}</Clave>
            <Parametros>
                <idEmpresa>{request.idEmpresa}</idEmpresa>
                <referenciaItem>{request.referenciaItem}</referenciaItem>
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

        private Proveedor_Item data(string result)
        {
            var dataSet = XElement.Parse(result);

            var proveedorItem = new Proveedor_Item();

            foreach (XElement xe in dataSet.Elements())
            {
                proveedorItem.nitProveedor = xe.Element("nitProveedor").Value;
                proveedorItem.codigoSucursal = xe.Element("codigoSucursal").Value;
            }

            return proveedorItem;
        }

    }

    public class Rq_Proveedor_Item : IRequest<Proveedor_Item>
    {
        public string idEmpresa { get; set; }
        public string referenciaItem { get; set; }
    }

    public class Proveedor_Item
    {
        public string nitProveedor { get; set; }
        public string codigoSucursal { get; set; }
    }

}
