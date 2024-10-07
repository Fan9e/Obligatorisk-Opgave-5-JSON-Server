using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;

class TcpJsonServer
{
    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 8888);//opretter en TCP-lytter på alle nætvrkadresser
        listener.Start();//strter servern så den begnder at lytte
        Console.WriteLine("Server started...");

        while (true) //Starter lykke og pga (true) køre den evigt
        {
            TcpClient client = listener.AcceptTcpClient();//den skal acceptere forbindelsen og retunere TCPClient
            Task.Run(() => HandleClient(client)); // Concurrent== kan håndtere flere klienter afgangen
        }
    }

    static void HandleClient(TcpClient client)
	{
		NetworkStream ns = client.GetStream();//henter nætværksstream fra klienten
		StreamReader reader = new StreamReader(ns);//opretter reader til at læse data
		StreamWriter writer = new StreamWriter(ns) { AutoFlush = true };//opretter en writer til at skrive data
        //AutoFlush = true sikrer, at data sendes med det samme

        try
        {
			string jsonInput = reader.ReadLine();//læser en linje fra klienten og gemmer i jsoninput
			Console.WriteLine($"Received: {jsonInput}");//siger recived + jsoninput beskeden

			// Parse the JSON object
			var request = JsonSerializer.Deserialize<JsonRequest>(jsonInput);//omdanner den JSON-Streng den ar modtager og laver til en JsonRequest-object
			string result = "";//initalisere en variable til at gemme resultatet

			// Check om jason-objektet er null eller mangler de requestede dataer
			if (request == null || string.IsNullOrWhiteSpace(request.Method) || request.Tal1 == null || request.Tal2 == null)
			{
				var errorResponse = new JsonResponse { Status = "error", Message = "Invalid request format" };//hvis forspørgslen er ugyldig send fejl besked
				writer.WriteLine(JsonSerializer.Serialize(errorResponse));//omdanner fra Json-objeckt til json-streng og sender fejl respons/ aka srialiserer
				return;
			}

			// Behandler requeston forhold til hvad man har bedt den om og øre et loop
			switch (request.Method.ToLower())
			{
				case "random":
					Random random = new Random();
					result = random.Next(request.Tal1.Value, request.Tal2.Value + 1).ToString();
					break;
				case "add":
					result = (request.Tal1.Value + request.Tal2.Value).ToString();
					break;
				case "subtract":
					result = (request.Tal1.Value - request.Tal2.Value).ToString();
					break;
				default:
					var invalidMethodResponse = new JsonResponse { Status = "error", Message = "Invalid method" };
					writer.WriteLine(JsonSerializer.Serialize(invalidMethodResponse));
					return;
			}

			// Send success response
			var response = new JsonResponse { Status = "success", Result = result };//Svaret bliver lavet og gemt ned i response stadg som object 

            writer.WriteLine(JsonSerializer.Serialize(response));//omdanner json-objectet(response) til en streng som writer behandler
			Console.WriteLine($"Result sent: {result}");//kan så blive udskrevet som result
		}
		catch (Exception e)//hvsnoget går galt bliver der kastet en execption
		{
			Console.WriteLine($"Error: {e.Message}");//error + messege udskrevet
		}
		finally
		{
            // Lukker forbindelsen til klienten, når håndteringen er afsluttet eller ved en fejl.

            client.Close();
		}
	}
}

// klasse JSON-forespørgsel, som klienten sender til serveren.
public class JsonRequest
{
	public string Method { get; set; }
	public int? Tal1 { get; set; }
	public int? Tal2 { get; set; }
}

// Klasse JSON-svar, som serveren sender tilbage til klienten.
public class JsonResponse
{
	public string Status { get; set; }
	public string Result { get; set; }
	public string Message { get; set; }
}

