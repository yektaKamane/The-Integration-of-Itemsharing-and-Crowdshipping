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
        //private List<AlgorithmTesting.AssignedTriple> triples = new List<AlgorithmTesting.AssignedTriple>();
        //private List<AlgorithmTesting.Trip> trips = new List<AlgorithmTesting.Trip>();
        private List<List<AlgorithmTesting.AssignedTriple>> initial_population = new List<List<AlgorithmTesting.AssignedTriple>>();
       
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

        public static void remove_from_list_of_nodes(List<AlgorithmTesting.Node> list, AlgorithmTesting.Node node)
        {
            int i = 0;
            while (i< list.Count)
            {
                if (list[i].x == node.x && list[i].y == node.y) 
                {
                    list.RemoveAt(i);
                }
                i++;
            }
        }
        public static void remove_from_list_of_trips(List<AlgorithmTesting.Trip> list, AlgorithmTesting.Trip trip)
        {
            int i = 0;
            while (i < list.Count)
            {
                if (list[i].x_dest == trip.x_dest && list[i].y_dest == trip.y_dest && 
                    list[i].x_src == trip.x_src && list[i].y_src == trip.y_src)
                {
                    list.RemoveAt(i);
                }
                i++;
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

        public bool check_feasibility(AlgorithmTesting.AssignedTriple assigned, int n)
        {
            // assigned is the object we wanna check feasiblity
            // n is the type of assignment
            double speed = 30.0; // km per hour
            if (n == 0)
            {
                // check ssrc condition i.e.  t < 10 min
                double distance = GetDistance(assigned.source.y, assigned.source.x, assigned.destination.y, assigned.destination.x);
                double time = distance / speed;
                if (time < 10)
                {                    
                    // 10 is the ssrc flexibility
                    assigned.type_of_delivery = 1;
                    return true;
                }
            }
            if (n == 1)
            {
                // check home_del condition
                double direct_trip_distance = GetDistance(assigned.crowdshipper.y_src, assigned.crowdshipper.x_src, assigned.crowdshipper.y_dest, assigned.crowdshipper.x_dest);
                double direct_trip_duration = direct_trip_distance / speed;
                double detour_distance = GetDistance(assigned.crowdshipper.y_src, assigned.crowdshipper.x_src, assigned.source.y, assigned.source.x)
                                   + GetDistance(assigned.source.y, assigned.source.x, assigned.destination.y, assigned.destination.x)
                                   + GetDistance(assigned.destination.y, assigned.destination.x, assigned.crowdshipper.y_dest, assigned.crowdshipper.x_dest)
                                   - direct_trip_distance;
                double detour_duration = detour_distance / speed;
                if (0.2 * direct_trip_duration > detour_duration)
                {
                    assigned.type_of_delivery = 2;
                    return true;
                }

            }

            if (n == 2)
            {
                // check neighborhood_del conditions
                double direct_trip_distance = GetDistance(assigned.crowdshipper.y_src, assigned.crowdshipper.x_src, assigned.crowdshipper.y_dest, assigned.crowdshipper.x_dest);
                double direct_trip_duration = direct_trip_distance / speed;
                double detour_distance = GetDistance(assigned.crowdshipper.y_src, assigned.crowdshipper.x_src, assigned.source.y, assigned.source.x)
                                   + GetDistance(assigned.source.y, assigned.source.x, assigned.destination.y, assigned.destination.x)                                   
                                   - direct_trip_distance;
                double detour_duration = detour_distance / speed;

                double demander_distance = GetDistance(assigned.destination.y, assigned.destination.x, assigned.crowdshipper.y_dest, assigned.crowdshipper.x_dest);
                double demander_duration = demander_distance / speed;
                if ((0.2 * direct_trip_duration > detour_duration) && (demander_duration < 10))
                {
                    assigned.type_of_delivery = 3;
                    return true;
                }
            }

            return false;
        }

        public void Generate_Initial_Population(int population_size)
        {

            for(int i=0; i<population_size; i++)
            {
                // Get a copy from 6 lists
                // Supplies 
                List<AlgorithmTesting.Node> supplies = new List<AlgorithmTesting.Node>();
                for (int k=0; k< AlgorithmTesting.Program.supplier_nodes.Count; k++)
                {
                    var temp = new AlgorithmTesting.Node(AlgorithmTesting.Program.supplier_nodes[k].x, AlgorithmTesting.Program.supplier_nodes[k].y);
                    supplies.Add(temp);
                }
                // Requests 
                List<AlgorithmTesting.Node> requests = new List<AlgorithmTesting.Node>();
                for (int k = 0; k < AlgorithmTesting.Program.demander_nodes.Count; k++)
                {
                    var temp = new AlgorithmTesting.Node(AlgorithmTesting.Program.demander_nodes[k].x, AlgorithmTesting.Program.demander_nodes[k].y);
                    requests.Add(temp);
                }
                // Crowdshippers 
                List<AlgorithmTesting.Trip> crowdshippers = new List<AlgorithmTesting.Trip>();
                for (int k = 0; k < AlgorithmTesting.Program.crowdshipper_nodes.Count; k++)
                {
                    var temp = new AlgorithmTesting.Trip(AlgorithmTesting.Program.crowdshipper_nodes[k].x_src,
                        AlgorithmTesting.Program.crowdshipper_nodes[k].y_src, AlgorithmTesting.Program.crowdshipper_nodes[k].x_dest,
                        AlgorithmTesting.Program.crowdshipper_nodes[k].y_dest);
                    crowdshippers.Add(temp);
                }

                // Sup_Req
                List<AlgorithmTesting.AssignedTriple> sup_req = new List<AlgorithmTesting.AssignedTriple>();
                for (int k = 0; k < AlgorithmTesting.Program.tuples_sup_req.Count; k++)
                {
                    AlgorithmTesting.Node sup = new AlgorithmTesting.Node(AlgorithmTesting.Program.tuples_sup_req[k].source.x, AlgorithmTesting.Program.tuples_sup_req[k].source.y);
                    AlgorithmTesting.Node req = new AlgorithmTesting.Node(AlgorithmTesting.Program.tuples_sup_req[k].destination.x, AlgorithmTesting.Program.tuples_sup_req[k].destination.y);
                    var temp = new AlgorithmTesting.AssignedTriple(sup, req, null);
                    sup_req.Add(temp);
                }

                // Sup_Crowd
                List<AlgorithmTesting.AssignedTriple> sup_crowd = new List<AlgorithmTesting.AssignedTriple>();
                for (int k = 0; k < AlgorithmTesting.Program.tuples_sup_crowd.Count; k++)
                {
                    AlgorithmTesting.Node sup = new AlgorithmTesting.Node(AlgorithmTesting.Program.tuples_sup_crowd[k].source.x, AlgorithmTesting.Program.tuples_sup_crowd[k].source.y);
                    AlgorithmTesting.Trip crowd = new AlgorithmTesting.Trip(AlgorithmTesting.Program.tuples_sup_crowd[k].crowdshipper.x_src,
                                                                            AlgorithmTesting.Program.tuples_sup_crowd[k].crowdshipper.y_src,
                                                                            AlgorithmTesting.Program.tuples_sup_crowd[k].crowdshipper.x_dest,
                                                                            AlgorithmTesting.Program.tuples_sup_crowd[k].crowdshipper.y_dest);

                    var temp = new AlgorithmTesting.AssignedTriple(sup, null, crowd);
                    sup_crowd.Add(temp);
                }

                // Req_Crowd
                List<AlgorithmTesting.AssignedTriple> req_crowd = new List<AlgorithmTesting.AssignedTriple>();
                for (int k = 0; k < AlgorithmTesting.Program.tuples_req_crowd.Count; k++)
                {
                    AlgorithmTesting.Node req = new AlgorithmTesting.Node(AlgorithmTesting.Program.tuples_req_crowd[k].destination.x, AlgorithmTesting.Program.tuples_req_crowd[k].destination.y);
                    AlgorithmTesting.Trip crowd = new AlgorithmTesting.Trip(AlgorithmTesting.Program.tuples_req_crowd[k].crowdshipper.x_src,
                                                                            AlgorithmTesting.Program.tuples_req_crowd[k].crowdshipper.y_src,
                                                                            AlgorithmTesting.Program.tuples_req_crowd[k].crowdshipper.x_dest,
                                                                            AlgorithmTesting.Program.tuples_req_crowd[k].crowdshipper.y_dest);

                    var temp = new AlgorithmTesting.AssignedTriple(null, req, crowd);
                    req_crowd.Add(temp);
                }

                List<AlgorithmTesting.AssignedTriple> feasibleSolution = new List<AlgorithmTesting.AssignedTriple>();
                int len = supplies.Count; // let's assume all the lengths are the same (200)
                int j = 0;                
                while (j<10)
                {
                    if (supplies.Count == 0 && requests.Count == 0 && crowdshippers.Count == 0)
                    {
                        // if all the items are assigned get out of the loop
                        break;
                    }
                    // Randomly get a number between 0, 1, 2
                    // Self_Sourcing, Home_Delivery, Neighborhood_Delivery
                    int randomNum = random.Next(3);                    
                    if (randomNum == 0)
                    {                        
                        int rand = random.Next(sup_req.Count);                     
                        var selected = sup_req[rand];                                                
                        while (!check_feasibility(selected, 0))
                        {
                            rand = random.Next(sup_req.Count);
                            selected = sup_req[rand];
                        }
                        feasibleSolution.Add(selected);                        
                        sup_req.Remove(selected);
                        remove_from_list_of_nodes(supplies, selected.source);                        
                        remove_from_list_of_nodes(requests, selected.destination);                        
                    }
                    if (randomNum == 1)
                    {

                    }

                    j++;             

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