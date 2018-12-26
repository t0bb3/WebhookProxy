using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebhookProxy.Server.Controllers
{
    [Route("return")]
    [ApiController]
    public class ReturnController : Controller
    {

        [Route("timeout")]
        public async Task<ActionResult> Timeout()
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            return await Timeout();
        }

        [Route("200")]
        public ActionResult ReturnOk()
        {
            return Ok("Standard response for successful HTTP requests. The actual response will depend on the request method used. In a GET request, the response will contain an entity corresponding to the requested resource. In a POST request, the response will contain an entity describing or containing the result of the action.");
        }

        [Route("201")]
        public ActionResult ReturnCreated()
        {
            return Created("[uri]", new { StatusCode = 201, Status = "CREATED", Description = "The request has been fulfilled, resulting in the creation of a new resource." });
        }

        [Route("202")]
        public ActionResult ReturnAccepted()
        {
            return Accepted(new { StatusCode = 202, Status = "ACCEPTED", Description = "The request has been accepted for processing, but the processing has not been completed. The request might or might not be eventually acted upon, and may be disallowed when processing occurs." });
        }

        [Route("203")]
        public ActionResult ReturnNonAuthoritativeInformation()
        {
            return StatusCode(203, "The server is a transforming proxy (e.g. a Web accelerator) that received a 200 OK from its origin, but is returning a modified version of the origin's response.");
        }

        [Route("204")]
        public ActionResult ReturnNoContent()
        {
            return NoContent();
        }

        [Route("205")]
        public ActionResult ReturnResetContent()
        {
            return StatusCode(205, "The server successfully processed the request, but is not returning any content. Unlike a 204 response, this response requires that the requester reset the document view.");
        }

        [Route("206")]
        public ActionResult ReturnPartialContent()
        {
            return StatusCode(206, "The server is delivering only part of the resource (byte serving) due to a range header sent by the client. The range header is used by HTTP clients to enable resuming of interrupted downloads, or split a download into multiple simultaneous streams.");
        }

        [Route("207")]
        public ActionResult ReturnMultiStatus()
        {
            return StatusCode(207, "The message body that follows is by default an XML message and can contain a number of separate response codes, depending on how many sub-requests were made.");
        }

        [Route("208")]
        public ActionResult ReturnAlreadyReported()
        {
            return StatusCode(208, "The members of a DAV binding have already been enumerated in a preceding part of the (multistatus) response, and are not being included again.");
        }

        [Route("209")]
        public ActionResult ReturnIMUsed()
        {
            return StatusCode(209, "The server has fulfilled a request for the resource, and the response is a representation of the result of one or more instance-manipulations applied to the current instance.");
        }





        [Route("401")]
        public ActionResult SimulateUnaAuthorized()
        {
            return StatusCode(401);
        }

        [Route("403")]
        public ActionResult SimulateForbidden()
        {
            return StatusCode(403);
        }

        [Route("404")]
        public ActionResult SimulateNotFound()
        {
            return NotFound();
        }

        [Route("418")]
        public ActionResult SimulateTeacup()
        {
            return StatusCode(418, "m4y 8e sh0rt 4nd st0ut");
        }

        [Route("500")]
        public ActionResult SimulateError()
        {
            throw new Exception("Simulated exception");
        }
    }
}
