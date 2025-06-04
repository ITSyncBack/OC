using MediatR;
using System.Xml.Linq;
using Ts_Ws_Unoee;
using Pd_Ws_Unoee;

namespace Querys
{
    public class CriterioLineasQry : IRequestHandler<Rq_Criterio_Linea, List<Criterio_Linea>>
    {
        private readonly object _soapClient;

        public CriterioLineasQry(object soapClient)
        {
            _soapClient = soapClient;
        }

        public async Task<List<Criterio_Linea>> Handle(Rq_Criterio_Linea request, CancellationToken cancellationToken)
        {
            var xmlQuery = 
            $@"<Consulta>
            <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
            <IdCia>1</IdCia>
                <IdProveedor>Integraciones</IdProveedor>
                <IdConsulta>ItemCriterioLineas</IdConsulta>
                <Usuario>{Environment.GetEnvironmentVariable("user")}</Usuario>
                <Clave>{Environment.GetEnvironmentVariable("password")}</Clave>
                <Parametros>
                    <idsEmpresas>{request.idsEmpresas}</idsEmpresas>
                    <idCriterioProveedor>{request.idCriterioProveedor}</idCriterioProveedor>
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

        private List<Criterio_Linea> data(string result)
        {
            var dataSet = XElement.Parse(result);

            var criterioLineas = new List<Criterio_Linea>();

            foreach (XElement xe in dataSet.Elements())
            {
                criterioLineas.Add(
                    new Criterio_Linea
                    {
                        id = xe.Element("id").Value,
                        descripcion = xe.Element("descripcion").Value,
                        idEmpresa = xe.Element("idEmpresa").Value
                    }
                );
            }

            return criterioLineas;
        }

    }

    public class Rq_Criterio_Linea : IRequest<List<Criterio_Linea>>
    {
        public string idsEmpresas { get; set; }
        public string idCriterioProveedor { get; set; }
    }

    public class Criterio_Linea
    {
        public string id { get; set; }
        public string descripcion { get; set; }
        public string idEmpresa { get; set; }
    }
}
