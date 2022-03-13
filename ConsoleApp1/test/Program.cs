using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphAlgorithms;
using Genetic;
using System.Xml;
using OsmSharp.Streams;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;


namespace AlgorithmTesting
{
    public class Node
    { // Supply or Request objects
        public double x;
        public double y;
        // public int type;
        public double distance(Node a)
        {
            double result;
            double y_dis = Math.Abs(a.y - this.y);
            double x_dis = Math.Abs(a.x - this.x);
            result = Math.Sqrt(y_dis * y_dis + x_dis * x_dis);
            return result;

        }
        public Node(double a, double b)
        {
            this.x = a;
            this.y = b;
        }

    }

    public class Trip
    { // Supply or Request objects
        public double x_src;
        public double y_src;
        public double x_dest;
        public double y_dest;

        public Trip(double a, double b, double c, double d)
        {
            this.x_src = a;
            this.y_src = b;
            this.x_dest = c;
            this.y_dest = d;
        }

    }

    public class AssignedTriple
    {
        public Node source;
        public Node destination;
        public Trip crowdshipper;        

        public double abs_distance; // distance between src and dest (used for selfsourcing)
        public double home_del_detour; // 2(x+y) --> 2(supply to crowdshippers src + crowdshippers dest to req)
        public double profit;   // profit of the assignment : ( r_ssrc ) or ( r_home - c_dtr * t_dtr )
        public double cost;     // how much we must pay the crowdshipper : ( c_dtr * t_dtr )
        public int type_of_delivery;  // 0 : self source, 1 : home delivery, 2 : neighborhood delivery 

        // c_dtr = 30$ per hour
        // r_ssrc = 10$
        // r_home = 15$

        public AssignedTriple(Node a, Node b, Trip c)
        {
            this.source = a;
            this.destination = b;
            this.crowdshipper = c;
        }


    }

    public sealed class Program
    {
        static readonly Random random = new Random();
        static List<Node> supplier_nodes = new List<Node>();
        static List<Node> demander_nodes = new List<Node>();
        static int dataset_size = 0;
        static int data_samples = 200;

        static void get_data_size()
        {
            XmlTextReader reader = null;
            reader = new XmlTextReader(@"D:\Project_Data\Atlanta_City_Map\Atlanta.osm");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            while (reader.Read())
            {
                if (reader.Name.Equals("node") && reader.AttributeCount >= 2) { dataset_size++; }
            }
            Console.WriteLine("total count: {0}", dataset_size);
        }

        static void create_supplies() {
            // Supplies 
            string text = "";
            XmlTextReader reader = null;            
            for (int i = 0; i < data_samples; i++)
            {
                Console.WriteLine("supply {0}", i);
                reader = new XmlTextReader(@"D:\Project_Data\Atlanta_City_Map\Atlanta.osm");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                int randomNum = random.Next(dataset_size);
                while (randomNum > 0)
                {
                    reader.Read();
                    randomNum--;
                    if (randomNum == 0)
                    {
                        while (reader.Name != "node" || reader.AttributeCount < 2)
                        {
                            reader.Read();
                        }
                        text += reader.GetAttribute(7) + " " + reader.GetAttribute(8) + "\n";
                    }
                }
            }
            File.WriteAllText(@"D:\Project_Data\Generated_Coordinates\supplies.txt", text);
        }
        static void create_requests()
        {
            // Requests 
            string text = "";
            XmlTextReader reader = null;
            for (int i = 0; i < data_samples; i++)
            {
                Console.WriteLine("Request {0}", i);
                reader = new XmlTextReader(@"D:\Project_Data\Atlanta_City_Map\Atlanta.osm");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                int randomNum = random.Next(dataset_size);
                while (randomNum > 0)
                {
                    reader.Read();
                    randomNum--;
                    if (randomNum == 0)
                    {
                        while (reader.Name != "node" || reader.AttributeCount < 2)
                        {
                            reader.Read();
                        }
                        text += reader.GetAttribute(7) + " " + reader.GetAttribute(8) + "\n";
                    }
                }
            }
            File.WriteAllText(@"D:\Project_Data\Generated_Coordinates\requests.txt", text);
        }
        static void create_crowdshippers() 
        { 
            // Crowdshipper
            string text = "";
            XmlTextReader reader = null;
            for (int i = 0; i < data_samples; i++)
            {
                Console.WriteLine("Crowdshipper {0}", i);
                reader = new XmlTextReader(@"D:\Project_Data\Atlanta_City_Map\Atlanta.osm");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                int randomNum = random.Next(dataset_size);
                int randomNum1 = randomNum;
                while (randomNum > 0)
                {
                    reader.Read();
                    randomNum--;
                    if (randomNum == 0)
                    {
                        while (reader.Name != "node" || reader.AttributeCount < 2)
                        {
                            reader.Read();
                        }
                        text += reader.GetAttribute(7) + "," + reader.GetAttribute(8) + " ";
                    }
                }                
                int randomNum2 = random.Next(dataset_size);     
                while (randomNum2 == randomNum1)
                {
                    randomNum2 = random.Next(dataset_size);
                }
                reader = new XmlTextReader(@"D:\Project_Data\Atlanta_City_Map\Atlanta.osm");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                while (randomNum2 > 0)
                {
                    reader.Read();
                    randomNum2--;
                    if (randomNum2 == 0)
                    {
                        while (reader.Name != "node" || reader.AttributeCount < 2)
                        {
                            reader.Read();
                        }
                        text += reader.GetAttribute(7) + "," + reader.GetAttribute(8) + "\n";
                    }
                }
            }
            File.WriteAllText(@"D:\Project_Data\Generated_Coordinates\crowdshipper.txt", text);
        }
        static double[,] generateMatrix()
        {
            string[] supplies = System.IO.File.ReadAllLines(@"D:\Project_Data\Generated_Coordinates\supplies.txt");
            string[] demands = System.IO.File.ReadAllLines(@"D:\Project_Data\Generated_Coordinates\requests.txt");

            //Console.WriteLine(lines[0]);
            int demandslen = demands.Length;
            int supplieslen = supplies.Length;

            var matrix1 = new double[demandslen, supplieslen];

            if (demandslen == supplieslen)
            {                
                
                for (int i = 0; i < demandslen; i++)
                {
                    string[] supp_digits = supplies[i].Split(' ');           
                    double supp_x = Convert.ToDouble(supp_digits[0]);
                    double supp_y = Convert.ToDouble(supp_digits[1]);

                    var node = new Node(supp_x, supp_y);
                    supplier_nodes.Add(node);

                    for (int j=0; j < supplieslen; j++)
                    {
                        string[] dem_digits = demands[j].Split(' ');
                        double demand_x = Convert.ToDouble(dem_digits[0]);
                        double demand_y = Convert.ToDouble(dem_digits[1]);

                        if (i == 0)
                        {
                            var node1 = new Node(demand_x, demand_y);
                            demander_nodes.Add(node1);
                        }

                        double x = Math.Abs(supp_x - demand_x);
                        double y = Math.Abs(supp_y - demand_y);

                        matrix1[i, j] = Math.Sqrt(x * x + y * y);
                    }

                }
            }

            return matrix1;
        }

        static void printMatrix(double[,] matrix)
        {
            Console.WriteLine("Matrix:");
            var size = matrix.GetLength(0);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                    Console.Write("{0,5:0}", matrix[i, j]);
                Console.WriteLine();
            }
        }

        static void printArray(double[] array)
        {
            Console.WriteLine("Array:");
            var size = array.Length;
            for (int i = 0; i < size; i++)
                    Console.Write("{0,5:0}", array[i]);
            Console.WriteLine();
        }

        static List<AssignedTriple> create_ssrc_tuples(double[] array)
        {
            List<AssignedTriple> result = new List<AssignedTriple>();
            for (int i=0; i<array.Length; i++)
            {
                Node source = supplier_nodes[i];
                Node destination = demander_nodes[(int)array[i]];
                var assignedTuple = new AssignedTriple(source, destination, null);
                result.Add(assignedTuple);
            }
            return result;
        }


        static double resultValue(double[] array, double[,] matrix)
            // to calculate the value of selfsourcing
            // needs to be changed tho
        {
            double resultVal = 0;
            for (int i=0; i < array.Length; i++)
            {
                resultVal += matrix[i, (int)array[i]];
            }       
            return resultVal;
        }

        static int Main()
        {
            //get_data_size();
            //create_supplies();
            //create_requests();
            //create_crowdshippers();

           
            var matrix = generateMatrix();
            var hunAlgorithm = new HungarianAlgorithm(matrix);
            var result = hunAlgorithm.Run();
            //printArray(result);

            // This list holds tuples of assigned sources and destinations
            // while it would be better if there were 3 nodes in the assignment triple 
            // and one of them would be the crowdshipper --> diff kind of node

            List<AssignedTriple> tuples = create_ssrc_tuples(result);

            var genAlgorithm = new GeneticAlgorithm(tuples);

            // This should change to -> var result = genAlgorithm.Run();
            genAlgorithm.Run();

            Console.ReadKey();
            return 0;
        }
    }
}
