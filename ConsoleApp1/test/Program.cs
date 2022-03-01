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
        public double home_del_detour; // 
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

        public void calculate_profit()
        {
            double x = Math.Abs(source.x - destination.x);
            double y = Math.Abs(source.y - destination.y);
            this.abs_distance = Math.Sqrt(x * x + y * y);       


        }

    }

    public class Program
    {

        static List<Node> supplier_nodes = new List<Node>();
        static List<Node> demander_nodes = new List<Node>();        

        static void generate_data_file()
        {
            Random random = new Random();
            XmlTextReader reader = null;
            reader = new XmlTextReader(@"D:\Atlanta.osm");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            int count = 0;
            while (reader.Read())
            {
                if (reader.Name.Equals("node") && reader.AttributeCount == 4) { count++; }
            }
            Console.WriteLine("total count: {0}", count);
            int data_samples = 2;

            // Supplies 
            reader = new XmlTextReader(@"D:\Atlanta.osm");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            string text = "";
            for (int i=0; i<data_samples; i++)
            {
                int randomNum = random.Next(count);
                Console.WriteLine("random number: {0}", randomNum);
                while (randomNum > 0)
                {
                    reader.Read();
                    if (reader.Name.Equals("node") && reader.AttributeCount == 4) 
                    { 
                        randomNum--;                        
                    }
                    if (randomNum == 0)
                    {
                        text += reader.GetAttribute(1) + " " + reader.GetAttribute(2) + "\n";
                    }
                }
            }
            File.WriteAllText(@"D:\supplies.txt", text);

            Console.WriteLine("dx");
            
        }
        static double[,] generateMatrix()
        {
            string[] supplies = System.IO.File.ReadAllLines(@"D:\final project test cases\245\generator_result\!supplies_and_types.txt");
            string[] demands = System.IO.File.ReadAllLines(@"D:\final project test cases\245\generator_result\!demands_and_types.txt");

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
            generate_data_file();
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
            // genAlgorithm.Run();

            Console.ReadKey();
            return 0;
        }
    }
}
