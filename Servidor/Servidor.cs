/******************************************************************************************************************************************
 * Practica07-Servidor
 * Stalin Garcia
 * Fecha de realizacion: 25/06/2024
 * Fecha de Entrega: 26/06/2024
 * Resultados:
 * - Se realizaron los cambios solicitados, moviendo los metodos que se pidieron a la nueva clase MiProtocolo en el proyecto Protocolo
 *
 *
 * Conclusiones:
 *  - La reestructuración del código para separar la lógica de negocios en la clase MiProtocolo dentro de Protocolo.cs ha mejorado 
 *    significativamente la organización y claridad del servidor. Esto facilita la mantenibilidad y la extensión futura del código.
 *  - Aunque el servidor actual maneja excepciones básicas relacionadas con operaciones de socket, se recomienda implementar un manejo 
 *    más exhaustivo de errores para cubrir escenarios específicos y mejorar la robustez del sistema frente a fallos inesperados.
 * 
 * Recomendaciones:
 *  - Es fundamental asegurar una gestión adecuada de recursos como NetworkStream y TcpClient, asegurando su cierre apropiado en todas las
 *    situaciones, incluyendo manejo de excepciones para evitar posibles fugas de recursos y mantener la estabilidad del servidor.
 *  - Se sugiere integrar un sistema de registro (logging) dentro del servidor para registrar eventos importantes como conexiones entrantes, 
 *    errores de red y actividades del protocolo. Esto facilitará la monitorización y depuración del servidor en tiempo real.
 *
 ******************************************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Protocolo; // Importa el espacio de nombres del protocolo definido en Protocolo.cs

namespace Servidor {
    class Servidor {
        private static MiProtocolo protocolo = new MiProtocolo(); // Instancia del protocolo definido en Protocolo.cs
        private static TcpListener escuchador;
        private static Dictionary<string, int> listadoClientes = new Dictionary<string, int>(); // Listado de clientes conectados

        static void Main(string[] args) {
            try {
                escuchador = new TcpListener(IPAddress.Any, 8080); // Crea un listener TCP en todas las interfaces en el puerto 8080
                escuchador.Start(); // Inicia el listener
                Console.WriteLine("Servidor inició en el puerto 5000..."); // Mensaje de inicio

                while (true) {
                    TcpClient cliente = escuchador.AcceptTcpClient(); // Acepta conexiones entrantes
                    Console.WriteLine("Cliente conectado, puerto: {0}", cliente.Client.RemoteEndPoint.ToString()); // Muestra el cliente conectado
                    Thread hiloCliente = new Thread(ManipuladorCliente); // Crea un hilo para manejar al cliente
                    hiloCliente.Start(cliente); // Inicia el hilo para manejar al cliente
                }
            } catch (SocketException ex) {
                Console.WriteLine("Error de socket al iniciar el servidor: " +
                    ex.Message); // Manejo de excepciones de socket
            } finally {
                escuchador?.Stop(); // Detiene el listener si está en ejecución
            }
        }

        private static void ManipuladorCliente(object obj) {
            TcpClient cliente = (TcpClient)obj; // Convierte el objeto recibido en un cliente TCP
            NetworkStream flujo = null;
            try {
                flujo = cliente.GetStream(); // Obtiene el flujo de red del cliente
                byte[] bufferTx;
                byte[] bufferRx = new byte[1024]; // Buffer para recibir datos
                int bytesRx;

                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0) {
                    string mensajeRx =
                        Encoding.UTF8.GetString(bufferRx, 0, bytesRx); // Convierte los bytes recibidos a string
                    Pedido pedido = Pedido.Procesar(mensajeRx); // Procesa el mensaje recibido en un objeto Pedido
                    Console.WriteLine("Se recibió: " + pedido); // Muestra el pedido recibido en consola

                    string direccionCliente =
                        cliente.Client.RemoteEndPoint.ToString(); // Obtiene la dirección del cliente
                    Respuesta respuesta = protocolo.ResolverPedido(pedido, direccionCliente, listadoClientes); // Resuelve el pedido usando el protocolo definido
                    Console.WriteLine("Se envió: " + respuesta); // Muestra la respuesta enviada en consola

                    bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString()); // Convierte la respuesta en bytes para enviarla
                    flujo.Write(bufferTx, 0, bufferTx.Length); // Envía la respuesta al cliente
                }

            } catch (SocketException ex) {
                Console.WriteLine("Error de socket al manejar el cliente: " + ex.Message); // Manejo de excepciones de socket
                //se realiza los cambios hechos en clase
                flujo?.Close();
                cliente?.Close();
            } finally {
                
                //flujo?.Close();  // Cierre del flujo de red (comentado para evitar cierre prematuro)
                //cliente?.Close();  // Cierre del cliente TCP (comentado para evitar cierre prematuro)
            }
        }

    }
}
