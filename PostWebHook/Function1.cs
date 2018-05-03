
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace PostWebHook
{//De la base de datos de pratica monedas vamos a subir el precio de las monedas por su id
    public static class Function1
    {
        [FunctionName("FunctionWebHook")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req
            , TraceWriter log, ExecutionContext context)
        {
            log.Info("Aplicación para cambiar el precio de las monedas por su id, con WebHook");

            //Capturamos el Id de la moneda
            string idmoneda = req.Query["Id"];
            //Extraemos el contenido del Id
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            idmoneda = idmoneda ?? data?.idmoneda;
            //Comprobamos si nos ha enviado el parámetro Id
            if (idmoneda == null)
            {
                return new BadRequestObjectResult("Se necesita un Id de una moneda");
            }

            //Configuramos los elementos para el acceso a bbdd
            var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();


            //string cadenaconexion = @"Data Source=LOCALHOST\SQLTAJAMAR;Initial Catalog=Monedas;Integrated Security=True";
            string cadenaconexion = config.GetConnectionString("cadenahospital");
            SqlConnection cn = new SqlConnection(cadenaconexion);
            string consultaupdate = "UPDATE Monedas SET Precio = Precio + 1 WHERE Id=" + idmoneda;
            SqlCommand com = new SqlCommand
            {
                Connection = cn,
                CommandType = CommandType.Text,
                CommandText = consultaupdate
            };

            //Abrimos conexión para ejecutar la consulta
            cn.Open();
            com.ExecuteNonQuery();
            cn.Close();

            //Ahora mostramos los cambios de la moneda
            string consultasql = "SELECT * FROM Monedas WHERE Id=" + idmoneda;
            SqlDataAdapter ad = new SqlDataAdapter(consultasql, cadenaconexion);
            DataSet ds = new DataSet();
            ad.Fill(ds, "Monedas");
            
            //Hacemos la comprobación de si el Id proporcionado en la URL coincide
            //con el Id de alguna moneda de la base de datos, para devolver los datos,
            //o el mensaje de error
            if (ds.Tables["Monedas"].Rows.Count == 0)
            {
                return new BadRequestObjectResult("El Id proporcionado no es válido");
            }
            else
            {
                DataRow fila = ds.Tables["Monedas"].Rows[0];
                string mensaje = "La moneda " + fila["Descripcion"] + ", ha incrementado su precio a " + fila["Precio"];

                return (ActionResult)new OkObjectResult(mensaje);
            }
        }
    }
}
