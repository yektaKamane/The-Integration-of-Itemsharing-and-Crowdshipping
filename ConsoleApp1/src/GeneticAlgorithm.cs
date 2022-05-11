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
                Console.WriteLine("\nsource: ({0}, {1}), dest: ({2}, {3})",
                    list[i].source.x, list[i].source.y, list[i].destination.x, list[i].destination.y);
                if (list[i].crowdshipper != null)
                {
                    Console.WriteLine("trip: ({0}, {1}) -> ({2}, {3})",
                    list[i].crowdshipper.x_src, list[i].crowdshipper.y_src, list[i].crowdshipper.x_dest, list[i].crowdshipper.y_dest);
                }
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

        public static void remove_from_list_of_triples(List<AlgorithmTesting.AssignedTriple> list, AlgorithmTesting.AssignedTriple triple, int x)
        {
            //Console.WriteLine("tripl:\n src: {0}, {1} dest: {2}, {3} trip: ({4}, {5}) -> ({6}, {7})",
            //    triple.source.x, triple.source.y, triple.destination.x, triple.destination.y, triple.crowdshipper.x_src, triple.crowdshipper.y_src
            //    , triple.crowdshipper.x_dest, triple.crowdshipper.y_dest);
            
            int i = 0;
            if (x == 1)
            {
                while (i < list.Count)
                {
                    if (list[i].source.x == triple.source.x && list[i].source.y == triple.source.y &&                        
                        list[i].crowdshipper.x_dest == triple.crowdshipper.x_dest && list[i].crowdshipper.x_src == triple.crowdshipper.x_src &&
                        list[i].crowdshipper.y_dest == triple.crowdshipper.y_dest && list[i].crowdshipper.y_src == triple.crowdshipper.y_src)
                    {
                        list.RemoveAt(i);
                    }
                    i++;
                }
            }
            if(x == 2) 
            {
                while (i < list.Count)
                {
                    if (list[i].destination.x == triple.destination.x && list[i].destination.y == triple.destination.y &&
                        list[i].crowdshipper.x_dest == triple.crowdshipper.x_dest && list[i].crowdshipper.x_src == triple.crowdshipper.x_src &&
                        list[i].crowdshipper.y_dest == triple.crowdshipper.y_dest && list[i].crowdshipper.y_src == triple.crowdshipper.y_src)
                    {
                        list.RemoveAt(i);
                    }
                    i++;
                }
            }
            
        }

        public static double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            var res = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
            return (res + 0.3 * res) / 1000;
        }
        static void calculate_profit(AlgorithmTesting.AssignedTriple t)
        {
            double profit = 0.0;           
            double speed = 30.0; // km per hour
            if (t.type_of_delivery == 1)
            {
                // self source
                t.profit = 10;
            }
            
            if (t.type_of_delivery == 2)
            {
                double direct_trip_distance = GetDistance(t.crowdshipper.y_src, t.crowdshipper.x_src, t.crowdshipper.y_dest, t.crowdshipper.x_dest);
                double direct_trip_duration = direct_trip_distance / speed;
                double detour_distance = GetDistance(t.crowdshipper.y_src, t.crowdshipper.x_src, t.source.y, t.source.x)
                                   + GetDistance(t.source.y, t.source.x, t.destination.y, t.destination.x)
                                   + GetDistance(t.destination.y, t.destination.x, t.crowdshipper.y_dest, t.crowdshipper.x_dest)
                                   - direct_trip_distance;
                double detour_duration = detour_distance / speed;
                profit = 15 - (30 * detour_duration);
            }
            if (t.type_of_delivery == 3)
            {
                double direct_trip_distance = GetDistance(t.crowdshipper.y_src, t.crowdshipper.x_src, t.crowdshipper.y_dest, t.crowdshipper.x_dest);
                double direct_trip_duration = direct_trip_distance / speed;
                double detour_distance = GetDistance(t.crowdshipper.y_src, t.crowdshipper.x_src, t.source.y, t.source.x)
                                   + GetDistance(t.source.y, t.source.x, t.destination.y, t.destination.x)
                                   - direct_trip_distance;
                double detour_duration = detour_distance / speed;
                profit = 15 - (30 * detour_duration);
            }
            if (t.type_of_delivery == 4)
            {
                profit = 0;
            }
            t.profit = profit;

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

        static List<AlgorithmTesting.AssignedTriple> make_a_copy(List<AlgorithmTesting.AssignedTriple> original_list)
        {
            List<AlgorithmTesting.AssignedTriple> new_list = new List<AlgorithmTesting.AssignedTriple>();

            for (int i=0; i<original_list.Count; i++)
            {
                AlgorithmTesting.Node source = new AlgorithmTesting.Node(original_list[i].source.x, original_list[i].source.y);
                AlgorithmTesting.Node dest = new AlgorithmTesting.Node(original_list[i].destination.x, original_list[i].destination.y);
                AlgorithmTesting.Trip crowdshipper = null;

                if (original_list[i].crowdshipper != null)
                {
                    crowdshipper = new AlgorithmTesting.Trip(original_list[i].crowdshipper.x_src, original_list[i].crowdshipper.y_src, original_list[i].crowdshipper.x_dest, original_list[i].crowdshipper.y_dest);
                    
                }
                AlgorithmTesting.AssignedTriple assign = new AlgorithmTesting.AssignedTriple(source, dest, crowdshipper);
                assign.type_of_delivery = original_list[i].type_of_delivery;
                calculate_profit(assign);
                new_list.Add(assign);
            }
            return new_list;
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
                if (time * 60.0 < 10)
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
                if ((0.2 * direct_trip_duration > detour_duration) && (demander_duration * 60.0 < 10))
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
                while (true)
                {
                    if (supplies.Count == 0 || requests.Count == 0 || crowdshippers.Count == 0)
                    {
                        // if all the items are assigned get out of the loop
                        break;
                    }
                    // Randomly get a number between 0, 1, 2
                    // Self_Sourcing, Home_Delivery, Neighborhood_Delivery
                    int randomNum = random.Next(3);
                    //Console.WriteLine(randomNum);
                    if (randomNum == 0)
                    {
                        int counter = 0;
                        int rand = random.Next(sup_req.Count);                     
                        var selected = sup_req[rand];                                                
                        while (!check_feasibility(selected, 0))
                        {
                            counter++;
                            if (counter > population_size)
                            {
                                selected.type_of_delivery = 4;
                                break;
                            }
                            rand = random.Next(sup_req.Count);
                            selected = sup_req[rand];
                        }
                        calculate_profit(selected);
                        feasibleSolution.Add(selected);                        
                        sup_req.Remove(selected);
                        remove_from_list_of_nodes(supplies, selected.source);                        
                        remove_from_list_of_nodes(requests, selected.destination);                        
                    }
                    if (randomNum == 1)
                    {
                        // don't forget to add the selected whatever to the assignment 
                        // before checking feasibility
                        int counter = 0;
                        int rand = random.Next(req_crowd.Count);
                        var selected_req_crowd = req_crowd[rand];

                        int rand2 = random.Next(supplies.Count);
                        var selected_sup = supplies[rand2];
                        selected_req_crowd.source = selected_sup;                        

                        while (!check_feasibility(selected_req_crowd, 1))
                        {
                            counter++;
                            if (counter > population_size)
                            {
                                selected_req_crowd.type_of_delivery = 4;
                                break;
                            }
                            rand2 = random.Next(supplies.Count);
                            selected_sup = supplies[rand2];
                            selected_req_crowd.source = selected_sup;
                        }
                        calculate_profit(selected_req_crowd);
                        feasibleSolution.Add(selected_req_crowd);
                        req_crowd.Remove(selected_req_crowd);                                            
                        remove_from_list_of_nodes(supplies, selected_req_crowd.source);
                        remove_from_list_of_nodes(requests, selected_req_crowd.destination);
                        remove_from_list_of_trips(crowdshippers, selected_req_crowd.crowdshipper);
                        // also remove from sup_crowd    
                        AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(selected_req_crowd.source, selected_req_crowd.destination, selected_req_crowd.crowdshipper);
                        remove_from_list_of_triples(sup_crowd, temp, 1);
                    }

                    if (randomNum == 2)
                    {
                        // don't forget to add the selected whatever to the assignment 
                        // before checking feasibility
                        int counter = 0;
                        int rand = random.Next(sup_crowd.Count);
                        var selected_sup_crowd = sup_crowd[rand];

                        int rand2 = random.Next(requests.Count);
                        var selected_req = requests[rand2];
                        selected_sup_crowd.destination = selected_req;

                        while (!check_feasibility(selected_sup_crowd, 2))
                        {
                            counter++;
                            if (counter > population_size)
                            {
                                selected_sup_crowd.type_of_delivery = 4;
                                break;
                            }
                            rand2 = random.Next(requests.Count);
                            selected_req = requests[rand2];
                            selected_sup_crowd.destination = selected_req;
                        }
                        calculate_profit(selected_sup_crowd);
                        feasibleSolution.Add(selected_sup_crowd);
                        sup_crowd.Remove(selected_sup_crowd);
                        remove_from_list_of_nodes(supplies, selected_sup_crowd.source);
                        remove_from_list_of_nodes(requests, selected_sup_crowd.destination);
                        remove_from_list_of_trips(crowdshippers, selected_sup_crowd.crowdshipper);
                        // also remove from req_crowd    
                        AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(selected_sup_crowd.source, selected_sup_crowd.destination, selected_sup_crowd.crowdshipper);
                        remove_from_list_of_triples(req_crowd, temp, 2);
                    }

                    j++;             

                }

                // Add the i-th list to the population list
                initial_population.Add(feasibleSolution);
                //print_list(feasibleSolution, 10);
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
                //Console.WriteLine(sum);
            }
        }
        public void Select_Parents()
        {
            // Rank the chromosomes base on their fittness value
            // Only the half top of the population go to the next generation

            List<int> index_keeper = new List<int>(); // holds the original indexes
            for (int i=0; i< fitness_values.Count(); i++)
            {
                index_keeper.Add(i);
            }

            // Sort them according to fittness value
            int n = fitness_values.Count();
            for (int j=0; j < n - 1; j++)
            {
                for(int k=0; k < n - j - 1; k++)
                {
                    if(fitness_values[k] < fitness_values[k + 1])  // Descending order
                    {
                        double temp = fitness_values[k];
                        fitness_values[k] = fitness_values[k + 1];
                        fitness_values[k + 1] = temp;

                        int itemp = index_keeper[k];
                        index_keeper[k] = index_keeper[k + 1];
                        index_keeper[k + 1] = itemp;

                    }
                }
            }
            Console.WriteLine("\n------------\n New Gen \n");            
            for (int x=0; x<n; x++)
            {
                Console.WriteLine("index: {0}, fitness: {1}", index_keeper[x], fitness_values[x]);
            }

            // Create Next Generation
            List<List<AlgorithmTesting.AssignedTriple>> next_gen = new List<List<AlgorithmTesting.AssignedTriple>>();

            for(int i=0; i<n/2; i++)
            {
                // get the selected parent
                var member_orig = initial_population[index_keeper[i]];
                var member_copy = make_a_copy(member_orig);
                List<List<AlgorithmTesting.AssignedTriple>> members = new List<List<AlgorithmTesting.AssignedTriple>>();
                members.Add(member_orig);
                members.Add(member_copy);

                // Create two new children
                for (int j=0; j< members.Count; j++) {                
                    int rand1 = random.Next(members[j].Count);
                    int rand2 = random.Next(members[j].Count);
                    while (rand1 == rand2)
                    {
                        rand2 = random.Next(members[j].Count);
                    }

                    var assignment1 = members[j][rand1];
                    var assignment2 = members[j][rand2];

                    if (assignment1.type_of_delivery == 1 && assignment2.type_of_delivery != 1)
                    {
                        //Console.WriteLine("ssrc & home");
                        AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment1.destination, assignment2.crowdshipper);
                        AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment2.destination, null);
                        if (check_feasibility(child1, assignment2.type_of_delivery - 1))
                        {
                            members[j][rand1] = child1;                            
                        }
                        if (check_feasibility(child2, 0))
                        {
                            members[j][rand2] = child2;                           
                        }
                    }
                    if (assignment2.type_of_delivery == 1 && assignment1.type_of_delivery != 1)
                    {
                        //Console.WriteLine("ssrc & home");
                        AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment1.destination, null);
                        AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment2.destination, assignment1.crowdshipper);
                        if (check_feasibility(child1, assignment2.type_of_delivery - 1))
                        {
                            members[j][rand1] = child1;
                        }
                        if (check_feasibility(child2, 0))
                        {
                            members[j][rand2] = child2;
                        }
                    }

                    if (assignment1.type_of_delivery !=1 && assignment2.type_of_delivery != 1)
                    {
                        AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment1.destination, assignment2.crowdshipper);
                        AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment2.destination, assignment1.crowdshipper);
                        if (check_feasibility(child1, assignment2.type_of_delivery - 1))
                        {
                            members[j][rand1] = child1;
                        }
                        if (check_feasibility(child2, assignment2.type_of_delivery - 1))
                        {
                            members[j][rand2] = child2;
                        }
                    }
                    next_gen.Add(members[j]);
                }
                                        
            }
            initial_population = next_gen;

    }
        public void Create_Next_Generation()
        {
            // Do the mutation according to the list produced by the previous function
        }



        public void Run()
        {            

            int population_size = 100;
            int number_of_iterations = 100;            

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

