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
        public Node(double a, double b)
        {
            this.x = a;
            this.y = b;
        }
    }

    public class Trip
    { // Crowdshipping objects
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
    { // An assignment of supply, request and crowdshipper
        public Node source;
        public Node destination;
        public Trip crowdshipper;

        public int type_of_delivery;  // 0: not_set, 1 : self source, 2 : home delivery, 3 : neighborhood delivery, 4: infeasible
        public double abs_distance;   // distance between src and dest (used for selfsourcing)
        public double crowdshipper_detour; // in case of home_del: _ , neighborhood: _
        public double profit;   // profit of the assignment : ( r_ssrc ) or ( r_home - c_dtr * t_dtr )
        public double cost;     // how much we must pay the crowdshipper : ( c_dtr * t_dtr )
        
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
        public static List<Node> supplier_nodes = new List<Node>();
        public static List<Node> demander_nodes = new List<Node>();
        public static List<Trip> crowdshipper_nodes = new List<Trip>();

        public static List<AssignedTriple> tuples_sup_req = new List<AssignedTriple>();
        public static List<AssignedTriple> tuples_sup_crowd = new List<AssignedTriple>();
        public static List<AssignedTriple> tuples_req_crowd = new List<AssignedTriple>();

        // Change this out of hardcode
        //static int data_samples = 200;
        static int get_data_size()
        {
            int dataset_size = 0;
            XmlTextReader reader = null;
            reader = new XmlTextReader(@"D:\Project_Data\Atlanta_City_Map\Atlanta.osm");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            while (reader.Read())
            {
                if (reader.Name.Equals("node") && reader.AttributeCount >= 2) { dataset_size++; }
            }
            Console.WriteLine("total count: {0}", dataset_size);
            return dataset_size;
        }

        static void create_supplies(int dataset_size, int data_samples, int index) 
        {
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
                        text += reader.GetAttribute(1) + " " + reader.GetAttribute(2) + "\n";
                    }
                }
            }
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\";
            // If directory does not exist, create it
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(dir + "supplies.txt", text);
        }
        static void create_requests(int dataset_size, int data_samples, int index)
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
                        text += reader.GetAttribute(1) + " " + reader.GetAttribute(2) + "\n";
                    }
                }
            }
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\";
            // If directory does not exist, create it
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(dir + "requests.txt", text);            
        }
        static void create_crowdshippers(int dataset_size, int data_samples, int index) 
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
                        text += reader.GetAttribute(1) + "," + reader.GetAttribute(2) + " ";
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
                        text += reader.GetAttribute(1) + "," + reader.GetAttribute(2) + "\n";
                    }
                }
            }
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\";
            // If directory does not exist, create it
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(dir + "crowdshipper.txt", text);            
        }

        static void create_supp_nodes()
        {
            string[] supplies = System.IO.File.ReadAllLines(@"D:\Project_Data\Generated_Coordinates\supplies.txt");
            int supplieslen = supplies.Length;
            for (int i = 0; i < supplieslen; i++)
            {
                string[] supp_digits = supplies[i].Split(' ');
                double supp_x = Convert.ToDouble(supp_digits[0]);
                double supp_y = Convert.ToDouble(supp_digits[1]);

                var node = new Node(supp_x, supp_y);                
                supplier_nodes.Add(node);
            }
        }
        static void create_req_nodes()
        {
            string[] demands = System.IO.File.ReadAllLines(@"D:\Project_Data\Generated_Coordinates\requests.txt");
            int demandslen = demands.Length;
            for (int i = 0; i < demandslen; i++)
            {
                string[] demand_digits = demands[i].Split(' ');
                double demand_x = Convert.ToDouble(demand_digits[0]);
                double demand_y = Convert.ToDouble(demand_digits[1]);

                var node = new Node(demand_x, demand_y);          
                demander_nodes.Add(node);
            }
        }
        static void create_trips()
        {
            string[] crowdshippers = System.IO.File.ReadAllLines(@"D:\Project_Data\Generated_Coordinates\crowdshipper.txt");
            int crowdshipperslen = crowdshippers.Length;

            for (int i = 0; i < crowdshipperslen; i++)
            {
                string[] coords = crowdshippers[i].Split(' ');
                string[] src = coords[0].Split(',');
                double src_x = Convert.ToDouble(src[0]);
                double src_y = Convert.ToDouble(src[1]);

                string[] dest = coords[1].Split(',');
                double dest_x = Convert.ToDouble(dest[0]);
                double dest_y = Convert.ToDouble(dest[1]);

                var trip = new Trip(src_x, src_y, dest_x, dest_y);                
                crowdshipper_nodes.Add(trip);
            }
        }

        static double[,] generateMatrix_sup_req()
        {
            int supplieslen = supplier_nodes.Count;
            int demandslen = demander_nodes.Count;            
            var matrix = new double[supplieslen, demandslen];
            for (int i=0; i<supplieslen; i++)
            {
                for (int j=0; j<demandslen; j++)
                {
                    matrix[i, j] = Genetic.GeneticAlgorithm.GetDistance(supplier_nodes[i].y, supplier_nodes[i].x, demander_nodes[j].y, demander_nodes[j].x);
                }
            }            
            return matrix;
        }

        static double[,] generateMatrix_sup_crowd()
        {
            int supplieslen = supplier_nodes.Count;
            int crowdslen = crowdshipper_nodes.Count;
            var matrix = new double[supplieslen, crowdslen];
            for (int i = 0; i < supplieslen; i++)
            {
                for (int j = 0; j < crowdslen; j++)
                {
                    matrix[i, j] = Genetic.GeneticAlgorithm.GetDistance(supplier_nodes[i].y, supplier_nodes[i].x, crowdshipper_nodes[j].y_src, crowdshipper_nodes[j].x_src);
                }
            }
            return matrix;
        }

        static double[,] generateMatrix_req_crowd()
        {
            int demandslen = demander_nodes.Count;
            int crowdslen = crowdshipper_nodes.Count;
            var matrix = new double[demandslen, crowdslen];
            for (int i = 0; i < demandslen; i++)
            {
                for (int j = 0; j < crowdslen; j++)
                {
                    matrix[i, j] = Genetic.GeneticAlgorithm.GetDistance(demander_nodes[i].y, demander_nodes[i].x, crowdshipper_nodes[j].y_src, crowdshipper_nodes[j].x_src);
                }
            }
            return matrix;
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
                double distance = Genetic.GeneticAlgorithm.GetDistance(source.y, source.x, destination.y, destination.x);
                var assignedTuple = new AssignedTriple(source, destination, null);
                //Console.WriteLine("source {0} : {1} {2}\nassigned to request{3}: {4}, {5}", i, source.x, source.y, array[i], destination.x, destination.y);
                //Console.WriteLine("distance: {0}, time: {1}, profit: {2}", distance, distance / 30.0 * 60.0, 10);
                if (distance / 30.0 * 60.0 < 10)
                { // if detour time is less than f_dtr
                    assignedTuple.abs_distance = distance;
                    assignedTuple.type_of_delivery = 1;
                    assignedTuple.profit = 10;
                }
                
                result.Add(assignedTuple);
            }
            return result;
        }

        static List<AssignedTriple> create_sup_crowd_tuples(double[] array)
        {
            // Type of delivery is unset in here
            List<AssignedTriple> result = new List<AssignedTriple>();
            for (int i = 0; i < array.Length; i++)
            {
                Node supply = supplier_nodes[i];
                Trip crowdshipper = crowdshipper_nodes[(int)array[i]];
                double distance = Genetic.GeneticAlgorithm.GetDistance(supply.y, supply.x, crowdshipper.y_src, crowdshipper.x_src);
                var assignedTuple = new AssignedTriple(supply, null, crowdshipper);
                //Console.WriteLine("source {0} : {1} {2}\nassigned to request{3}: {4}, {5}", i, source.x, source.y, array[i], destination.x, destination.y);
                //Console.WriteLine("distance: {0}, time: {1}, profit: {2}", distance, distance/30.0, 10);
                result.Add(assignedTuple);
            }
            return result;
        }
        static List<AssignedTriple> create_req_crowd_tuples(double[] array)
        {
            // Type of delivery is unset in here
            List<AssignedTriple> result = new List<AssignedTriple>();
            for (int i = 0; i < array.Length; i++)
            {
                Node req = demander_nodes[i];
                Trip crowdshipper = crowdshipper_nodes[(int)array[i]];
                double distance = Genetic.GeneticAlgorithm.GetDistance(req.y, req.x, crowdshipper.y_dest, crowdshipper.x_dest);
                var assignedTuple = new AssignedTriple(null, req, crowdshipper);
                //Console.WriteLine("source {0} : {1} {2}\nassigned to request{3}: {4}, {5}", i, source.x, source.y, array[i], destination.x, destination.y);
                //Console.WriteLine("distance: {0}, time: {1}, profit: {2}", distance, distance/30.0, 10);
                result.Add(assignedTuple);
            }
            return result;
        }

        static void print_ssrc_result(List<AssignedTriple> list)
        {
            double profit = 0.0;
            for (int i=0; i< list.Count; i++)
            {
                if (list[i].type_of_delivery == 1)
                {
                    profit += list[i].profit;
                }
            }
            Console.WriteLine("total ssrc profit: {0}", profit);
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

        static void data_generator(int number_of_datasets, int number_of_instances)
        {
            var size = get_data_size();
            for (int i=0; i<number_of_datasets; i++)
            {
                create_supplies(size, number_of_instances, i+1);
                create_requests(size, number_of_instances, i+1);
                create_crowdshippers(size, number_of_instances, i+1);
            }
        }

        static int Main()
        {            

            // Create data
            // 10 datasets of size 200
            // data_generator(10, 200);


            // Reading data from the file
            // and store in lists

            // Gotta make a loop here
            create_supp_nodes();
            create_req_nodes();
            create_trips();


            // Running the Hungarian on Supply and Requests
            var matrix_sup_req = generateMatrix_sup_req();            
            var hunAlgorithm_sup_req = new HungarianAlgorithm(matrix_sup_req);
            var result_sup_req = hunAlgorithm_sup_req.Run();
            tuples_sup_req = create_ssrc_tuples(result_sup_req);
            print_ssrc_result(tuples_sup_req);


            // Running the Hungarian on Supply and Crowdshipper
            var matrix_sup_crowd = generateMatrix_sup_crowd();
            var hunAlgorithm_sup_crowd = new HungarianAlgorithm(matrix_sup_crowd);
            var result_sup_crowd = hunAlgorithm_sup_crowd.Run();
            tuples_sup_crowd = create_sup_crowd_tuples(result_sup_crowd);

            // Running the Hungarian on Requests and Crowdshipper
            var matrix_req_crowd = generateMatrix_req_crowd();
            var hunAlgorithm_req_crowd = new HungarianAlgorithm(matrix_sup_crowd);
            var result_req_crowd = hunAlgorithm_req_crowd.Run();
            tuples_req_crowd = create_req_crowd_tuples(result_req_crowd);


            var genAlgorithm = new GeneticAlgorithm();

            // This should change to -> var result = genAlgorithm.Run();
            genAlgorithm.Run();

            Console.ReadKey();
            return 0;
        }
    }
}
