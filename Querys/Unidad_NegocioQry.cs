using Ts_Ws_Unoee;
using Pd_Ws_Unoee;
using MediatR;
using System.Xml.Linq;

namespace Querys
{
    public class Unidad_NegocioQry : IRequestHandler<Rq_Unidad_Negocio, List<Unidad_Negocio>>
    {
        private readonly object _soapClient;
        public Unidad_NegocioQry(object soapClient)
        {
            _soapClient = soapClient;
        }

        public async Task<List<Unidad_Negocio>> Handle(Rq_Unidad_Negocio request, CancellationToken cancellationToken)
        {
            var xmlQuery =
            $@"<?xml version='1.0' encoding='utf-8'?>
            <Consulta>
            <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
            <IdCia>1</IdCia>
            <IdProveedor>Integraciones</IdProveedor>
            <IdConsulta>Unidadesdenegocio</IdConsulta>
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

        private List<Unidad_Negocio> data(string result)
        {
            var dataSet = XElement.Parse(result);

            var unidadesNegocio = new List<Unidad_Negocio>();

            foreach (XElement xe in dataSet.Elements())
            {
                unidadesNegocio.Add(
                    new Unidad_Negocio
                    {
                        id = xe.Element("id").Value,
                        descripcion = xe.Element("descripcion").Value,
                        idEmpresa = xe.Element("idEmpresa").Value
                    });
            }

            return unidadesNegocio;
        }   

    }

    public class Rq_Unidad_Negocio : IRequest<List<Unidad_Negocio>>{
        public string idsEmpresas { get; set; }
    }

    public class Unidad_Negocio
    {
        public string id { get; set; }
        public string descripcion { get; set; }
        public string idEmpresa { get; set; }
    }
}
