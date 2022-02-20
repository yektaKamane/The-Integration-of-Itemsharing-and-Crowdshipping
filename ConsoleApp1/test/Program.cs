using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphAlgorithms;

namespace AlgorithmTesting
{
    class Node
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

    class Trip
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

    class AssignedTriple
    {
        public Node source;
        public Node destination;
        public Trip crowdshipper;
        public double distance;
        public AssignedTriple(Node a, Node b, Trip c)
        {
            this.source = a;
            this.destination = b;
            this.crowdshipper = c;
            double x = Math.Abs(a.x - b.x);
            double y = Math.Abs(a.y - b.y);
            // This is the distance between src and destination 
            // Where's the crowdshipping dist???
            this.distance = Math.Sqrt(x * x + y * y);
        }

    }

    public class Program
    {

        static List<Node> supplier_nodes = new List<Node>();
        static List<Node> demander_nodes = new List<Node>();
        
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
                Console.WriteLine("equal size");
                
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
            var matrix = generateMatrix();
            var algorithm = new HungarianAlgorithm(matrix);
            var result = algorithm.Run();
            printArray(result);

            // This list holds tuples of assigned sources and destinations
            // while it would be better if there were 3 nodes in the assignment triple 
            // and one of them would be the crowdshipper --> diff kind of node

            List<AssignedTriple> tuples = create_ssrc_tuples(result);

            for (int i=0; i<tuples.Count; i++)
            {
                Console.WriteLine("src: {0} {1}, dest: {2} {3} ... dist: {4}", tuples[i].source.x, tuples[i].source.y, tuples[i].destination.x, tuples[i].destination.y, tuples[i].distance);
            }


            Console.ReadKey();
            return 0;
        }
    }
}
