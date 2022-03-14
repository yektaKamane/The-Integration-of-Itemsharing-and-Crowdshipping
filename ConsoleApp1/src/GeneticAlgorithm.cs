using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Osrm.Client.Models;
using Osrm.Client.v5;

namespace Genetic
{
    public sealed class GeneticAlgorithm
    {
        static HttpClient client = new HttpClient();
        private readonly Random random = new Random();

        private List<double> fitness_values = new List<double>();                
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
                Console.WriteLine("source: ({0}, {1}), dest: ({2}, {3}), trip: ({4}, {5}) -> ({6}, {7}) - profit : {8}",
                    list[i].source.x, list[i].source.y, list[i].destination.x, list[i].destination.y,
                    list[i].crowdshipper.x_src, list[i].crowdshipper.y_src, list[i].crowdshipper.x_dest, list[i].crowdshipper.y_dest,
                    list[i].profit);
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


        public static double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
        static void calculate_profit(AlgorithmTesting.AssignedTriple t)
        {
            double total_distance = 0.0;
            double total_travel_time = 0.0;
            double profit = 0.0;
            double dist1 = 0.0;
            double dist2 = 0.0;
            double avg_speed = 30.0; // km per hour

            if (t.crowdshipper != null)
            {               
                // the crowdshipper has to visit the source in both
                // home and neighborhood delivery
                dist1 = GetDistance(t.crowdshipper.y_src, t.crowdshipper.x_src, t.source.y, t.source.x);                

                // if it's a home delivery then there's a dist2
                dist2 = GetDistance(t.crowdshipper.y_dest, t.crowdshipper.x_dest, t.destination.y, t.destination.x);
            }
            total_distance = (2 * dist1) + (2 * dist2);            
            total_travel_time = (total_distance) / avg_speed;            
            profit = 15 - (1 * total_travel_time);
            t.profit = profit;
            //Console.WriteLine("total dist: {0}", total_distance);
            //Console.WriteLine("total time: {0}", total_travel_time);
            //Console.WriteLine("total profit: {0}", profit);
        }

        static void get_traveltime_api(AlgorithmTesting.AssignedTriple t)
        { // Using router.project calculates the travel time between some nodes
            if (t.crowdshipper != null)
            {
                var osrm5x = new Osrm5x("http://router.project-osrm.org/");
                
                var locations = new Osrm.Client.Location[] {
                    new Osrm.Client.Location(t.crowdshipper.x_src, t.crowdshipper.y_src),
                    new Osrm.Client.Location(t.source.x, t.source.y),
                };

                var result = osrm5x.Route(locations);
                Console.WriteLine(result.Routes[0].Duration);
            }                                   
        }

        public bool check_trip_feasibility(AlgorithmTesting.Trip trip, int n)
        {            
            //Console.WriteLine("trip : {0}, {1} -> {2}, {3}", trip.y_src, trip.x_src, trip.y_dest, trip.x_dest);
            // n is index of assignment in the triples list            
            // if type is home delivery:
            double direct_trip_duration = GetDistance(trip.y_src, trip.x_src, trip.y_dest, trip.x_dest) / 30.0;
            //double detour_duration = GetDistance(trip.y_src, trip.x_src, triples[n].source.y, triples[n].source.x) / 30.0;
            double detour_duration = GetDistance(trip.y_src, trip.x_src, triples[n].source.y, triples[n].source.x)
                                   + GetDistance(triples[n].source.y, triples[n].source.x, triples[n].destination.y, triples[n].destination.x)
                                   + GetDistance(triples[n].destination.y, triples[n].destination.x, trip.y_dest, trip.x_dest)
                                   - direct_trip_duration;
            detour_duration /= 30.0;
            // f_dtr_k = 0.2 * tk
            //Console.WriteLine("direct_trip_duration: {0}\ndetour_duration: {1}\nf_dtr_k: {2}", direct_trip_duration, detour_duration, 0.2 * direct_trip_duration);
            if (0.2 * direct_trip_duration > detour_duration)
            {
                Console.WriteLine(n);
                Console.WriteLine("direct_trip_duration: {0}\ndetour_duration: {1}\nf_dtr_k: {2}", direct_trip_duration, detour_duration, 0.2 * direct_trip_duration);
                return true;
            }
            return false;
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
                        // bring the next 3 lines out of the if condition
                        var source = new AlgorithmTesting.Node(triples[j].source.x, triples[j].source.y);
                        var destination = new AlgorithmTesting.Node(triples[j].destination.x, triples[j].destination.y);
                        var assignmentTemp = new AlgorithmTesting.AssignedTriple(source, destination, selectedTrip);
                        calculate_profit(assignmentTemp);

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
                // print_list(feasibleSolution, 10);
                // print_list_list(initial_population, 5);
            }
            //print_list_list(initial_population, 5);

        }

        public void Calculate_Fitness()
        {
            // Do this simultaneously with the generation creation
            // The above comment is old I dont know why I wrote that but I think I was refering
            // to the profit of each assignment. Then yeah I am calculating it inside the generation creation function
            fitness_values.Clear();            
            for (int i=0; i<initial_population.Count(); i++)
            {
                double sum = 0.0;
                for(int j=0; j < initial_population[i].Count(); j++)
                {
                    sum += initial_population[i][j].profit;
                }
                fitness_values.Add(sum);
                // Console.WriteLine(sum);
            }
        }
        public void Select_Parents()
        {
            // Create a list that says how many of each of the members should be replicated
            List<double> expected_counts = new List<double>();
            List<double> actual_counts = new List<double>();

            double sum_fitness = fitness_values.Sum();
            double avg_fitness = sum_fitness / fitness_values.Count();

            for(int i=0; i< fitness_values.Count(); i++)
            {
                double val = fitness_values[i] / avg_fitness;
                expected_counts.Add(val);                
                actual_counts.Add(Math.Round(val, MidpointRounding.AwayFromZero));
                //Console.WriteLine("{0}, {1}, {2}", fitness_values[i], expected_counts[i], actual_counts[i]);
            }
        }
        public void Create_Next_Generation()
        {
            // Do the mutation according to the list produced by the previous function
        }



        public void Run()
        {            

            int population_size = 100;
            int number_of_iterations = 1;            

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



//{ 
//    "code":"Ok",
//    "waypoints":[
//        { "hint":"sNEFiT3o4YAYAAAABQAAAAAAAAAgAAAASjFaQdLNK0AAAAAAsPePQQwAAAADAAAAAAAAABAAAAAa6QAA_kvMAKlYIQM8TMwArVghAwAA7wo65UwK",
//            "distance":4.231666,"location":[13.388798,52.517033],"name":"Friedrichstraße"},
//        { "hint":"7RDigOQFCIkGAAAACgAAAAAAAAB2AAAAW7-PQOKcyEAAAAAApq6DQgYAAAAKAAAAAAAAAHYAAAAa6QAAf27MABiJIQOCbswA_4ghAwAAXwU65UwK","distance":2.789393,"location":[13.397631,52.529432],"name":"Torstraße"}
//    ],
//        "routes":[{"weight_name":"routability","geometry":"mfp_I__vpAqJ`@wUrCa\\dCgGig@{DwW","weight":254.8,"distance":1884.7,"duration":253.6}]}