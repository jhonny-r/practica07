// ************************************************************************
// Practica 07
// Jhonny Ruiz
// Fecha de realización: 04/12/2024
// Fecha de entrega: 04/12/2024
//
// Resultados:
// Pudimos observar como se crea un cliente y un servidor en donde el cliente envia sus credenciales
// y el servidor las valida , luego de ello se envia los datos de placas de autos y si es valida
// se inidcara el dia de no circula y todo esto con mensajes de aviso en el servidor y una 
// interfaz grafica en el cliente por medio d un protocolo.
//
// conclusiones
//
//*Finalmente pudimos comprobar y manejar GitHub como una herramienta que nos permite tener un mayor
// control y facilidad de uso con el desarrollo de software ya que podemos ir realizando cambios sin el miedo
// de dañar todo, sino que tenemos hasta la opción de regresar todo a como estaba bien.
//
//*En conclusión, en el mundo del desarrollo de softwares de vital importancia el usar aplicaciones como
//GitHub ya que por lo general la mayoría de proyectos se los realizara en conjunto por lo que con esta
//herramienta cada quien puede trabajar desde su pc sin el miedo a dañar el trabajo del otro.
//
//*Finalmente, pudimos comprobar como se puede juntar a GitHub con visual studio para con ello hacer el
//uso de lo mejor de ambas herramientas y con ello facilitar la administración de nuestros proyectos.
//
// Recomendaciones
//
// *Se recomienda comprender el código a fondo y cual es su funcionamiento primero antes de realizar
// cualquier cambio.
//
// *Se recomienda realizar los cursos sobre el manejo de GitHub para realizar la practica de forma correcta
// y rápida ya que esta practica requiere de buenas bases en el tema.
//
//*Se recomienda siempre tener un respaldo de las credenciales usadas para GitHub para poder ingresar desde
//cualquier pc a nuestros repositorios.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Protocolo;

namespace Servidor
{
    class Servidor
    {
        private static TcpListener escuchador;
        private static Dictionary<string, int> listadoClientes
            = new Dictionary<string, int>();

        static void Main(string[] args)
        {
            try
            {
                escuchador = new TcpListener(IPAddress.Any, 8080);
                escuchador.Start();
                Console.WriteLine("Servidor inició en el puerto 5000...");

                while (true)
                {
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado, puerto: {0}", cliente.Client.RemoteEndPoint.ToString());
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error de socket al iniciar el servidor: " +
                    ex.Message);
            }
            finally 
            {
                escuchador?.Stop();
            }
        }

        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj;
            NetworkStream flujo = null;
            try
            {
                flujo = cliente.GetStream();
                byte[] bufferTx;
                byte[] bufferRx = new byte[1024];
                int bytesRx;

                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    string mensajeRx =
                        Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
                    Pedido pedido = Pedido.Procesar(mensajeRx);
                    Console.WriteLine("Se recibio: " + pedido);

                    string direccionCliente =
                        cliente.Client.RemoteEndPoint.ToString();
                    Respuesta respuesta = ResolverPedido(pedido, direccionCliente);
                    Console.WriteLine("Se envió: " + respuesta);

                    bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error de socket al manejar el cliente: " + ex.Message);
            }
            finally
            {
                flujo?.Close();
                cliente?.Close();
            }
        }

        private static Respuesta ResolverPedido(Pedido pedido, string direccionCliente)
        {
            Respuesta respuesta = new Respuesta
            { Estado = "NOK", Mensaje = "Comando no reconocido" };

            switch (pedido.Comando)
            {
                case "INGRESO":
               
                    if (
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        respuesta = new Random().Next(2) == 0
                            ? new Respuesta 
                            { Estado = "OK", 
                                Mensaje = "ACCESO_CONCEDIDO" }
                            : new Respuesta 
                            { Estado = "NOK", 
                                Mensaje = "ACCESO_NEGADO" };
                    }
                    else
                    {
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                case "CALCULO":
                    if (pedido.Parametros.Length == 3)
                    {
                        string modelo = pedido.Parametros[0];
                        string marca = pedido.Parametros[1];
                        string placa = pedido.Parametros[2];
                        if (ValidarPlaca(placa))
                        {
                            byte indicadorDia = ObtenerIndicadorDia(placa);
                            respuesta = new Respuesta
                            { Estado = "OK", 
                                Mensaje = $"{placa} {indicadorDia}" };
                            ContadorCliente(direccionCliente);
                        }
                        else
                        {
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                case "CONTADOR":
                    if (listadoClientes.ContainsKey(direccionCliente))
                    {
                        respuesta = new Respuesta
                        { Estado = "OK",
                            Mensaje = listadoClientes[direccionCliente].ToString() };
                    }
                    else
                    {
                        respuesta.Mensaje = "No hay solicitudes previas";
                    }
                    break;
            }

            return respuesta;
        }

        private static bool ValidarPlaca(string placa)
        {
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$");
        }

        private static byte ObtenerIndicadorDia(string placa)
        {
            int ultimoDigito = int.Parse(placa.Substring(6, 1));
            switch (ultimoDigito)
            {
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

        private static void ContadorCliente(string direccionCliente)
        {
            if (listadoClientes.ContainsKey(direccionCliente))
            {
                listadoClientes[direccionCliente]++;
            }
            else
            {
                listadoClientes[direccionCliente] = 1;
            }
        }

    }
}
