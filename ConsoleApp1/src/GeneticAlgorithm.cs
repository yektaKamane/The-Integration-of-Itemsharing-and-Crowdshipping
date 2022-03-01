using System;
using System.Collections.Generic;
using System.Linq;

namespace Genetic
{
    public sealed class GeneticAlgorithm
    {

        private readonly Random random = new Random();        
        private List<double> fitness = new List<double>();
        private List<double> expected_counts = new List<double>();
        
        private List<AlgorithmTesting.AssignedTriple> triples = new List<AlgorithmTesting.AssignedTriple>();
        private List<AlgorithmTesting.Trip> trips = new List<AlgorithmTesting.Trip>();
        private List<List<AlgorithmTesting.AssignedTriple>> initial_population = new List<List<AlgorithmTesting.AssignedTriple>>();

        public GeneticAlgorithm(List<AlgorithmTesting.AssignedTriple> a)
        {
            triples = a;
            // This initial population here has a null for all the trips
            // The actual initial population must be generated from this, 
            // after running the function create_tripslist()
        }

        public void print_list(List<AlgorithmTesting.AssignedTriple> list, int n)
        {
            int len = list.Count;
            if(n >= len)
            {
                n = len;
            }

            for(int i=0; i<n; i++)
            {
                Console.WriteLine("source: ({0}, {1}), dest: ({2}, {3}), trip: ({4}, {5}) -> ({6}, {7})",
                    list[i].source.x, list[i].source.y, list[i].destination.x, list[i].destination.y,
                    list[i].crowdshipper.x_src, list[i].crowdshipper.y_src, list[i].crowdshipper.x_dest, list[i].crowdshipper.y_dest);
            }
            Console.WriteLine("\n----\n\n");
        }

        public void print_list_list(List<List<AlgorithmTesting.AssignedTriple>> list, int n)
        {
            int len1 = list.Count;
            if (n >= len1)
            {
                n = len1;
            }

            for(int i=0; i< len1; i++)
            {
                Console.WriteLine("[\n");
                for(int j=0; j<list[i].Count; j++)
                {
                    Console.WriteLine("source: ({0}, {1}), dest: ({2}, {3}), trip: ({4}, {5}) -> ({6}, {7})",
                                        list[i][j].source.x, list[i][j].source.y, list[i][j].destination.x, list[i][j].destination.y,
                                        list[i][j].crowdshipper.x_src, list[i][j].crowdshipper.y_src, list[i][j].crowdshipper.x_dest, list[i][j].crowdshipper.y_dest);

                }
                Console.WriteLine("]\n");
            }

        }

        public void create_tripslist()
        {
            string[] crowdshippers = System.IO.File.ReadAllLines(@"D:\final project test cases\245\generator_result\read_only_crowdshipper.txt");
            int crowdshipperslen = crowdshippers.Length;

            for (int i=0; i< crowdshipperslen; i++)
            {
                string[] coords = crowdshippers[i].Split(' ');
                string[] src = coords[0].Split(',');
                double src_x = Convert.ToDouble(src[0]);
                double src_y = Convert.ToDouble(src[1]);

                string[] dest = coords[1].Split(',');
                double dest_x = Convert.ToDouble(dest[0]);
                double dest_y = Convert.ToDouble(dest[1]);

                var trip = new AlgorithmTesting.Trip(src_x, src_y, dest_x, dest_y);
                trips.Add(trip);
            }
        }

        public bool check_trip_feasibility(AlgorithmTesting.Trip trip, int n)
        {
            // n is index of assignment in the triples list            
            return true;
        }

        public void Generate_Initial_Population(int population_size)
        {

            for(int i=0; i<population_size; i++)
            {
                // Get a copy from the trips list
                List<AlgorithmTesting.Trip> tripsCloned = new List<AlgorithmTesting.Trip>();
                for (int k=0; k<trips.Count; k++)
                {
                    var temp = new AlgorithmTesting.Trip(trips[k].x_src, trips[k].y_src, trips[k].x_dest, trips[k].y_dest);
                    tripsCloned.Add(temp);
                }

                List<AlgorithmTesting.AssignedTriple> feasibleSolution = new List<AlgorithmTesting.AssignedTriple>();
                int tripsLen = tripsCloned.Count;                
                int j = 0;                
                while (tripsLen > 0)
                {
                    // Randomly get a member from the crowdshippers
                    // Pick a random number in range of tripsCloned                  
                    int randomNum = random.Next(tripsLen);                    
                    var selectedTrip = tripsCloned[randomNum];

                    // if the random crowdshipper is compatible with assignment j
                    // then make a new assignment object and add it to the list
                    // remove the crowdshipper from the copylist
                    if ( check_trip_feasibility(selectedTrip, j) )
                    {
                        //var source = triples[j].source;
                        var source = new AlgorithmTesting.Node(triples[j].source.x, triples[j].source.y);
                        var destination = new AlgorithmTesting.Node(triples[j].destination.x, triples[j].destination.y);
                        var assignmentTemp = new AlgorithmTesting.AssignedTriple(source, destination, selectedTrip);                        

                        feasibleSolution.Add(assignmentTemp);
                        tripsCloned.RemoveAt(randomNum);
                        tripsLen = tripsCloned.Count;
                        j++;
                    }

                    // if not compatible continue
                    else { continue; }                    

                }

                // Add the i-th list to the population list
                initial_population.Add(feasibleSolution);
                //print_list(feasibleSolution, 10);
                //print_list_list(initial_population, 5);
            }
            print_list_list(initial_population, 5);

        }

        public void Calculate_Fitness()
        {
            // Do this simultaneously with the generation creation
        }
        public void Select_Parents()
        {
            // Create a list that says how many of each of the members should be replicated
        }
        public void Create_Next_Generation()
        {
            // Do the mutation according to the list produced by the previous function
        }


        public void Run()
        {
            
            int population_size = 10;
            int number_of_iterations = 1000;

            create_tripslist(); // done    

            Generate_Initial_Population(population_size);
            // create as many possible assignments as the size given

            for(int i=0; i<number_of_iterations; i++)
            {
                Calculate_Fitness();
                // inside the initial pop and assigned object
                // calculate the value of fitness

                Select_Parents();
                // select as many chromosome as the size of initial pop

                Create_Next_Generation();
                // crossover and mutation baby B)


            }
        }


    }
}