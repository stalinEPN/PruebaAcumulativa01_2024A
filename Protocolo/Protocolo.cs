/******************************************************************************************************************************************
 * Practica07-Protocolo
 * Stalin Garcia
 * Fecha de realizacion: 25/06/2024
 * Fecha de Entrega: 26/06/2024
 * Resultados:
 * - Se realizaron los cambios solicitados, implementando la nueva clase MiProtocolo (nombre elegido por facilidad dado que al nombrarse Protocolo
 *   se obtenia un error en confusion de nombres) donde a su vez se implemento los metodos HazOperacion y ResolverPedido y todos los metodos que este
 *   ultimo implementa.
 *
 *
 * Conclusiones:
 *  - La centralización de la lógica del protocolo en la clase MiProtocolo ha mejorado la cohesión y claridad del código, permitiendo una 
 *    implementación más eficiente y reutilizable de las operaciones de negocio y comunicación.
 *  - La inclusión de métodos como ValidarPlaca y ObtenerIndicadorDia dentro de MiProtocolo facilita la reutilización de funcionalidades
 *    clave en diferentes partes del servidor y cliente, promoviendo una estructura de código más mantenible.
 * 
 * Recomendaciones:
 *  - Es recomendable incorporar comentarios detallados y documentación explicativa dentro del código de MiProtocolo, destacando el 
 *    propósito y funcionamiento de cada método para facilitar su comprensión y futuras modificaciones por otros desarrolladores.
 *  - Se sugiere implementar pruebas unitarias para validar exhaustivamente el comportamiento de MiProtocolo y sus métodos en diversos 
 *    escenarios de uso. Esto garantizará la fiabilidad y correcto funcionamiento del protocolo ante diferentes condiciones operativas.
 *
 ******************************************************************************************************************************************/
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;

namespace Protocolo {
    public class Pedido {
        public string Comando { get; set; }
        public string[] Parametros { get; set; }

        // Método estático para procesar un mensaje y convertirlo en un objeto Pedido
        public static Pedido Procesar(string mensaje) {
            var partes = mensaje.Split(' ');
            return new Pedido {
                Comando = partes[0].ToUpper(), // El primer segmento del mensaje es el comando, convertido a mayúsculas
                Parametros = partes.Skip(1).ToArray() // Los demás segmentos son parámetros del comando
            };
        }

        // Sobrescritura del método ToString para obtener una representación del Pedido como string
        public override string ToString() {
            return $"{Comando} {string.Join(" ", Parametros)}";
        }
    }

    public class Respuesta {
        public string Estado { get; set; }
        public string Mensaje { get; set; }

        // Sobrescritura del método ToString para obtener una representación de la Respuesta como string
        public override string ToString() {
            return $"{Estado} {Mensaje}";
        }
    }

    public class MiProtocolo {
        // Método para enviar un pedido y recibir una respuesta desde un flujo de red
        public Respuesta HazOperacion(Pedido pedido, NetworkStream flujo) {
            if (flujo == null) {
                return null; // Retorna null si el flujo de red es nulo
            }
            try {
                byte[] bufferTx = Encoding.UTF8.GetBytes(
                    pedido.Comando + " " + string.Join(" ", pedido.Parametros)); // Convierte el Pedido en bytes para enviarlo

                flujo.Write(bufferTx, 0, bufferTx.Length); // Escribe en el flujo de red

                byte[] bufferRx = new byte[1024]; // Buffer para recibir datos
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length); // Lee datos del flujo de red

                string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx); // Convierte los bytes recibidos a string

                var partes = mensaje.Split(' '); // Divide el mensaje en partes

                return new Respuesta {
                    Estado = partes[0], // El primer segmento es el estado de la respuesta
                    Mensaje = string.Join(" ", partes.Skip(1).ToArray()) // El resto es el mensaje de la respuesta
                };
            } catch (SocketException ex) {
                return new Respuesta {
                    Estado = "NOK", // Si hay una excepción de Socket, se devuelve una respuesta de error
                    Mensaje = "Error al intentar transmitir: " + ex.Message
                };
            }
        }

        // Método para resolver un pedido y generar una respuesta basada en el comando recibido
        public Respuesta ResolverPedido(Pedido pedido, string direccionCliente, Dictionary<string, int> listadoClientes) {
            Respuesta respuesta = new Respuesta {
                Estado = "NOK", // Estado inicial de la respuesta como "No reconocido"
                Mensaje = "Comando no reconocido"
            };

            switch (pedido.Comando) {
                case "INGRESO":
                    // Verifica credenciales de ingreso
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20") {
                        respuesta = new Random().Next(2) == 0
                            ? new Respuesta {
                                Estado = "OK", // Acceso concedido
                                Mensaje = "ACCESO_CONCEDIDO"
                            }
                            : new Respuesta {
                                Estado = "NOK", // Acceso denegado
                                Mensaje = "ACCESO_NEGADO"
                            };
                    } else {
                        respuesta.Mensaje = "ACCESO_NEGADO"; // Credenciales incorrectas
                    }
                    break;

                case "CALCULO":
                    // Realiza un cálculo basado en parámetros específicos
                    if (pedido.Parametros.Length == 3) {
                        string modelo = pedido.Parametros[0];
                        string marca = pedido.Parametros[1];
                        string placa = pedido.Parametros[2];
                        if (ValidarPlaca(placa)) { // Valida la placa del vehículo
                            byte indicadorDia = ObtenerIndicadorDia(placa); // Obtiene el indicador del día
                            respuesta = new Respuesta {
                                Estado = "OK", // Operación exitosa
                                Mensaje = $"{placa} {indicadorDia}"
                            };
                            ContadorCliente(direccionCliente, listadoClientes); // Registra el cliente para contar solicitudes
                        } else {
                            respuesta.Mensaje = "Placa no válida"; // Mensaje de error si la placa no es válida
                        }
                    }
                    break;

                case "CONTADOR":
                    // Obtiene el número de solicitudes realizadas por un cliente específico
                    if (listadoClientes.ContainsKey(direccionCliente)) {
                        respuesta = new Respuesta {
                            Estado = "OK", // Estado OK si se encuentra el cliente
                            Mensaje = listadoClientes[direccionCliente].ToString() // Número de solicitudes como mensaje
                        };
                    } else {
                        respuesta.Mensaje = "No hay solicitudes previas"; // No se encontró el cliente en el listado
                    }
                    break;
            }

            return respuesta; // Retorna la respuesta generada
        }

        // Método privado para validar el formato de una placa
        private bool ValidarPlaca(string placa) {
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$"); // Valida el formato AAA1111
        }

        // Método privado para obtener el indicador de día basado en el último dígito de la placa
        private byte ObtenerIndicadorDia(string placa) {
            int ultimoDigito = int.Parse(placa.Substring(6, 1));
            switch (ultimoDigito) {
                case 1:
                case 2:
                    return 0b00100000; // Lunes
                case 3:
                case 4:
                    return 0b00010000; // Martes
                case 5:
                case 6:
                    return 0b00001000; // Miércoles
                case 7:
                case 8:
                    return 0b00000100; // Jueves
                case 9:
                case 0:
                    return 0b00000010; // Viernes
                default:
                    return 0;
            }
        }

        // Método privado para contar las solicitudes de un cliente específico
        private void ContadorCliente(string direccionCliente, Dictionary<string, int> listadoClientes) {
            if (listadoClientes.ContainsKey(direccionCliente)) {
                listadoClientes[direccionCliente]++; // Incrementa el contador si el cliente ya existe
            } else {
                listadoClientes[direccionCliente] = 1; // Inicializa el contador si es un cliente nuevo
            }
        }
    }
}
