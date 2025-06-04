using Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Querys;

namespace Siesa.Api.Terceroo.Controllers
{
    [Route("[action]")]
    //[Route("oc/[action]")]
    [ApiController]
    public class OcController : ControllerBase
    {
        private readonly IMediator _mediator;
        public OcController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<Empresa>>> getEmpresas(){
            var empresas = await _mediator.Send(new Rq_Empresa { });
            return Ok(empresas);
        }
        
        [HttpGet("{idsEmpresas}")]
        public async Task<ActionResult<List<Unidad_Negocio>>> getUnidadesNegocio(string idsEmpresas){
            var unidadesNegocio = await _mediator.Send(new Rq_Unidad_Negocio { idsEmpresas = idsEmpresas });
            return Ok(unidadesNegocio);
        }

        [HttpGet("{idsEmpresas}")]
        public async Task<ActionResult<List<Criterio_proveedor>>> getCriterioProveedores(string idsEmpresas){
            var criterioProveedores = await _mediator.Send(new Request_criterio_proveedores { idsEmpresas = idsEmpresas });
            return Ok(criterioProveedores);
        }

        [HttpGet("{idsEmpresas}/{idCriterioProveedor}")]
        public async Task<ActionResult<List<Criterio_Linea>>> getCriterioLineas(string idsEmpresas, string idCriterioProveedor){
            var criterioLineas = await _mediator.Send(new Rq_Criterio_Linea { idsEmpresas = idsEmpresas, idCriterioProveedor = idCriterioProveedor });
            return Ok(criterioLineas);
        }

        [HttpPost]
        public async Task<ActionResult<List<Linea_Plan_Compras>>> getPlanCompras(Rq_Plan_Compras filtros)
        {
            var planCompras = await _mediator.Send(filtros);
            if(planCompras.Count == 0) return NoContent();
            return Ok(planCompras);
        }

        [HttpGet("{idEmpresa}/{referenciaItem}")]
        public async Task<ActionResult<Proveedor_Item>> getProveedorItem(string idEmpresa, string referenciaItem){
            var proveedorItem = await _mediator.Send(new Rq_Proveedor_Item { idEmpresa = idEmpresa, referenciaItem = referenciaItem });
            if (proveedorItem == null) return NoContent();
            return Ok(proveedorItem);
        }

        [HttpPost]
        public async Task<ActionResult<Unit>> createOC([FromBody] Rq_Create_Oc command){
            await _mediator.Send(command);
            return Ok(Unit.Value);
        }
    }
}
