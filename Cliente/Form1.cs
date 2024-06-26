/******************************************************************************************************************************************
 * Practica07-Cliente
 * Stalin Garcia
 * Fecha de realizacion: 25/06/2024
 * Fecha de Entrega: 26/06/2024
 * Resultados:
 * - Se realizaron los cambios solicitados, llevando el metodo correspondiente a la nueva clase MiProtocolo de tal manera que en el presente
 *   codigo solo se utilice el mismo metodo pero a travez de la instanciacion de un objeto de la nueva clase MiProtocolo.
 *
 *
 * Conclusiones:
 *  - El diseño de la interfaz de usuario proporciona una experiencia clara y amigable para el usuario final, ofreciendo retroalimentación 
 *    efectiva sobre el estado de las operaciones realizadas y las respuestas recibidas del servidor.
 *  - La implementación del protocolo de comunicación en la clase MiProtocolo facilita una separación clara de responsabilidades entre la 
 *    lógica de la aplicación y la gestión de la comunicación de red, promoviendo una arquitectura más modular y mantenible.
 * 
 * Recomendaciones:
 *  - Es esencial implementar un manejo robusto de apertura y cierre de conexiones (TcpClient y NetworkStream), asegurando su adecuada gestión 
 *    y liberación de recursos para prevenir posibles problemas de rendimiento y seguridad.
 *  - Se aconseja reforzar la validación de datos ingresados por el usuario, especialmente en campos críticos como usuario, contraseña, modelo, 
 *    marca y placa, para prevenir errores de formato y posibles vulnerabilidades de seguridad en la aplicación.
 *
 ******************************************************************************************************************************************/

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using Protocolo;  // Se importa el espacio de nombres del protocolo definido

namespace Cliente {
    public partial class FrmValidador : Form {
        private TcpClient remoto;    // Cliente TCP para la conexión con el servidor
        private NetworkStream flujo;  // Flujo de red para la comunicación con el servidor
        private MiProtocolo protocolo = new MiProtocolo();  // Instancia del protocolo definido

        public FrmValidador() {
            InitializeComponent();  // Inicialización de componentes del formulario
        }

        private void FrmValidador_Load(object sender, EventArgs e) {
            try {
                remoto = new TcpClient("127.0.0.1", 8080);  // Conexión al servidor en localhost, puerto 8080
                flujo = remoto.GetStream();  // Obtención del flujo de red para la comunicación
            } catch (SocketException ex) {
                MessageBox.Show("No se pudo establecer conexión " + ex.Message,
                    "ERROR");  // Manejo de excepción en caso de error de conexión
                //se realiza los cambios hechos en clase
                flujo?.Close();
                remoto?.Close();
            } finally {
                //flujo?.Close();  // Cierre del flujo de red (comentado para evitar cierre prematuro)
                //remoto?.Close();  // Cierre del cliente TCP (comentado para evitar cierre prematuro)
            }

            // Deshabilitar controles relacionados con la placa y los días de la semana
            panPlaca.Enabled = false;
            chkLunes.Enabled = false;
            chkMartes.Enabled = false;
            chkMiercoles.Enabled = false;
            chkJueves.Enabled = false;
            chkViernes.Enabled = false;
            chkDomingo.Enabled = false;
            chkSabado.Enabled = false;
        }

        private void btnIniciar_Click(object sender, EventArgs e) {
            string usuario = txtUsuario.Text;  // Obtener el nombre de usuario ingresado
            string contraseña = txtPassword.Text;  // Obtener la contraseña ingresada

            if (usuario == "" || contraseña == "") {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña",
                    "ADVERTENCIA");  // Validación de campos de usuario y contraseña vacíos
                return;
            }

            // Crear un pedido de ingreso con usuario y contraseña
            Pedido pedido = new Pedido {
                Comando = "INGRESO",
                Parametros = new[] { usuario, contraseña }
            };

            // Enviar el pedido al servidor y recibir la respuesta
            Respuesta respuesta = protocolo.HazOperacion(pedido, flujo);

            if (respuesta == null) {
                MessageBox.Show("Hubo un error", "ERROR");  // Manejo de error si no hay respuesta
                return;
            }

            // Según la respuesta del servidor, habilitar o deshabilitar paneles y mostrar mensajes
            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO") {
                panPlaca.Enabled = true;  // Habilitar panel de placa
                panLogin.Enabled = false;  // Deshabilitar panel de inicio de sesión
                MessageBox.Show("Acceso concedido", "INFORMACIÓN");  // Mostrar mensaje de acceso concedido
                txtModelo.Focus();  // Enfocar el campo de modelo
            } else if (respuesta.Estado == "NOK" && respuesta.Mensaje == "ACCESO_NEGADO") {
                panPlaca.Enabled = false;  // Deshabilitar panel de placa
                panLogin.Enabled = true;  // Habilitar panel de inicio de sesión
                MessageBox.Show("No se pudo ingresar, revise credenciales",
                    "ERROR");  // Mostrar mensaje de acceso denegado
                txtUsuario.Focus();  // Enfocar el campo de usuario
            }
        }

        private void btnConsultar_Click(object sender, EventArgs e) {
            string modelo = txtModelo.Text;  // Obtener el modelo ingresado
            string marca = txtMarca.Text;  // Obtener la marca ingresada
            string placa = txtPlaca.Text;  // Obtener la placa ingresada

            // Crear un pedido de cálculo con modelo, marca y placa
            Pedido pedido = new Pedido {
                Comando = "CALCULO",
                Parametros = new[] { modelo, marca, placa }
            };

            // Enviar el pedido al servidor y recibir la respuesta
            Respuesta respuesta = protocolo.HazOperacion(pedido, flujo);

            if (respuesta == null) {
                MessageBox.Show("Hubo un error", "ERROR");  // Manejo de error si no hay respuesta
                return;
            }

            // Según la respuesta del servidor, mostrar información o mensajes de error
            if (respuesta.Estado == "NOK") {
                MessageBox.Show("Error en la solicitud.", "ERROR");  // Mostrar mensaje de solicitud incorrecta
                // Desmarcar todos los días de la semana
                chkLunes.Checked = false;
                chkMartes.Checked = false;
                chkMiercoles.Checked = false;
                chkJueves.Checked = false;
                chkViernes.Checked = false;
            } else {
                var partes = respuesta.Mensaje.Split(' ');  // Dividir la respuesta en partes
                MessageBox.Show("Se recibió: " + respuesta.Mensaje,
                    "INFORMACIÓN");  // Mostrar la respuesta recibida del servidor

                byte resultado = Byte.Parse(partes[1]);  // Obtener el resultado de la respuesta

                // Según el resultado, marcar el día correspondiente
                switch (resultado) {
                    case 0b00100000:
                        chkLunes.Checked = true;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00010000:
                        chkMartes.Checked = true;
                        chkLunes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00001000:
                        chkMiercoles.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00000100:
                        chkJueves.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00000010:
                        chkViernes.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        break;
                    default:
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                }
            }
        }

        private void btnNumConsultas_Click(object sender, EventArgs e) {
            String mensaje = "hola";  // Mensaje de ejemplo

            // Crear un pedido de contador con el mensaje
            Pedido pedido = new Pedido {
                Comando = "CONTADOR",
                Parametros = new[] { mensaje }
            };

            // Enviar el pedido al servidor y recibir la respuesta
            Respuesta respuesta = protocolo.HazOperacion(pedido, flujo);

            if (respuesta == null) {
                MessageBox.Show("Hubo un error", "ERROR");  // Manejo de error si no hay respuesta
                return;
            }

            // Según la respuesta del servidor, mostrar información o mensajes de error
            if (respuesta.Estado == "NOK") {
                MessageBox.Show("Error en la solicitud.", "ERROR");  // Mostrar mensaje de solicitud incorrecta
            } else {
                var partes = respuesta.Mensaje.Split(' ');  // Dividir la respuesta en partes
                MessageBox.Show("El número de pedidos recibidos en este cliente es " + partes[0],
                    "INFORMACIÓN");  // Mostrar el número de pedidos recibidos
            }
        }

        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e) {
            if (flujo != null)
                flujo.Close();  // Cerrar el flujo de red si está abierto
            if (remoto != null)
                if (remoto.Connected)
                    remoto.Close();  // Cerrar el cliente TCP si está conectado
        }
    }
}
