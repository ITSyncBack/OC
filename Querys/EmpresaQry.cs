using Ts_Ws_Unoee;
using Pd_Ws_Unoee;
using MediatR;
using System.Xml.Linq;

namespace Querys
{
    public class EmpresaQry : IRequestHandler<Rq_Empresa, List<Empresa>>
    {
        private readonly object _soapClient;

        public EmpresaQry(object soapClient)
        {
            _soapClient = soapClient;
        }

        public async Task<List<Empresa>> Handle(Rq_Empresa request, CancellationToken cancellationToken)
        {
            var xmlQuery =
            $@"<?xml version='1.0' encoding='utf-8'?>
            <Consulta>
            <NombreConexion>{Environment.GetEnvironmentVariable("url")}</NombreConexion>
            <IdCia>1</IdCia>
            <IdProveedor>Integraciones</IdProveedor>
            <IdConsulta>Empresas</IdConsulta>
            <Usuario>{Environment.GetEnvironmentVariable("user")}</Usuario>
            <Clave>{Environment.GetEnvironmentVariable("password")}</Clave>
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

        private List<Empresa> data(string result)
        {
            var dataSet = XElement.Parse(result);

            var empresas = new List<Empresa>();

            foreach (XElement xe in dataSet.Elements())
            {
                empresas.Add(
                    new Empresa
                    {
                        id = xe.Element("id").Value,
                        razonSocial = xe.Element("razonSocial").Value
                    }
                );
            }

            return empresas;
        }

    }

    public class Rq_Empresa : IRequest<List<Empresa>>{
    } 

    public class Empresa {
        public string id { get; set; }
        public string razonSocial { get; set; }
    }
}
