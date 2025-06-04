using Ts_Ws_Unoee;
using Pd_Ws_Unoee;
using MediatR;
using System.Xml.Linq;

namespace Querys
{
    public class CriterioProveedoresQry : IRequestHandler<Request_criterio_proveedores, List<Criterio_proveedor>>
    {

        private readonly object _soapClient;

        public CriterioProveedoresQry(object soapClient)
        {
            _soapClient = soapClient;
        }
        public async Task<List<Criterio_proveedor>> Handle(Request_criterio_proveedores request, CancellationToken cancellationToken)
        {
            try
            {
                var xmlQuery = 
                $@"<Consulta>
                <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
                <IdCia>1</IdCia>
                <IdProveedor>Integraciones</IdProveedor>
                <IdConsulta>ItemCriterioProveedor</IdConsulta>
                <Usuario>{Environment.GetEnvironmentVariable("user")}</Usuario>
                <Clave>{Environment.GetEnvironmentVariable("password")}</Clave>
                <Parametros>
                    <idsEmpresas>{request.idsEmpresas}</idsEmpresas>
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
            catch (Exception ex)
            {
                throw new Exception();
            }
        }

        private List<Criterio_proveedor> data(string result)
        {
            var dataSet = XElement.Parse(result);

            var criterioProveedores = new List<Criterio_proveedor>();

            foreach (XElement xe in dataSet.Elements())
            {
                criterioProveedores.Add(
                    new Criterio_proveedor
                    {
                        id = xe.Element("id").Value,
                        descripcion = xe.Element("descripcion").Value,
                        idEmpresa = xe.Element("idEmpresa").Value
                    }
                );
            }

            return criterioProveedores;
        }

    }

    public class Request_criterio_proveedores : IRequest<List<Criterio_proveedor>>
    {
        public string idsEmpresas { get; set; }
    }

    public class Criterio_proveedor
    {
        public string id { get; set; }
        public string descripcion { get; set; }
        public string idEmpresa { get; set; }
    }

}
