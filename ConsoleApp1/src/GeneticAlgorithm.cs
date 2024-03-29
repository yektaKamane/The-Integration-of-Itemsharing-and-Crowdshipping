﻿using System;
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
       
        public void write_results_in_file(int index, int j)
        {
            // Write all the matches
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\Result" + j.ToString();
            //Console.WriteLine(initial_population[0].Count);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(dir + "\\assignments.txt", "");

            for (int i=0; i<initial_population[0].Count; i++)
            {
                string assignment = "";
                assignment += "\n" + (i + 1).ToString() + ".\n";
                assignment += "Type of delivery: ";
                int type = initial_population[0][i].type_of_delivery;
                if (type == 1) { assignment += "self-source\n"; }
                if (type == 2) { assignment += "home delivery\n"; }
                if (type == 3) { assignment += "neighborhood delivery\n"; }
                if (type == 4) { assignment += "Unmatched\n"; }
                assignment += "Profit: " + initial_population[0][i].profit + "\n";
                assignment += "Supplier: " + initial_population[0][i].source.x.ToString() + ", " + initial_population[0][i].source.y.ToString() + "\n";
                assignment += "Request: " + initial_population[0][i].destination.x.ToString() + ", " + initial_population[0][i].destination.y.ToString() + "\n";
                if(initial_population[0][i].crowdshipper == null)
                {
                    assignment += "Crowdshipper: -\n";
                }
                else
                {
                    assignment += "Crowdshipper: (" + initial_population[0][i].crowdshipper.x_src.ToString() + ", " + initial_population[0][i].crowdshipper.y_src.ToString() + ")"
                        + " -> (" + initial_population[0][i].crowdshipper.x_dest.ToString() + ", " + initial_population[0][i].crowdshipper.y_dest.ToString() + ")\n";                        
                }              
                File.AppendAllText(dir + "\\assignments.txt", assignment);
            }
            
        }

        public void Write_profit_iteration_file(int index, int i, int j)
        {
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\Result" + j.ToString();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }            
            string text = "i" + i.ToString() + " p" + fitness_values[0].ToString() + "\n";
            File.AppendAllText(dir + "\\profit-iteration.txt", text);


        }

        public void Write_ssrc_profit(int index, int j)
        {
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\Result" + j.ToString();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string text = "";
            File.AppendAllText(dir + "\\ssrc-profit.txt", text);
        }

        public void Write_type_iteration(int index, int i, int j)
        {
            string dir = @"D:\Project_Data\Generated_Coordinates\Sample" + index.ToString() + "\\Result" + j.ToString();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            double s = 0;
            double h = 0;
            double n = 0;
            double u = 0;
         
            for(int k=0; k<initial_population[0].Count; k++)
            {
                if (initial_population[0][k].type_of_delivery == 1) s++;
                if (initial_population[0][k].type_of_delivery == 2) h++;
                if (initial_population[0][k].type_of_delivery == 3) n++;
                if (initial_population[0][k].type_of_delivery == 4) u++;
            }
            string text = "i" + i.ToString() + " s" + s.ToString() + " h" + h.ToString() + " n" + n.ToString() + " u" + u.ToString() + "\n";
            File.AppendAllText(dir + "\\type-iteration.txt", text);
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
                    if (                      
                        list[i].crowdshipper.x_dest == triple.crowdshipper.x_dest && list[i].crowdshipper.x_src == triple.crowdshipper.x_src &&
                        list[i].crowdshipper.y_dest == triple.crowdshipper.y_dest && list[i].crowdshipper.y_src == triple.crowdshipper.y_src)
                    {
                        list.RemoveAt(i);                        
                    }
                    i++;
                }
            }

            if (x == 2)
            {
                while (i < list.Count)
                {
                    if (list[i].destination.x == triple.destination.x && list[i].destination.y == triple.destination.y)
                    {
                        list.RemoveAt(i);
                    }
                    i++;
                }
            }
            if (x == 3)
            {
                while (i < list.Count)
                {
                    if (list[i].source.x == triple.source.x && list[i].source.y == triple.source.y)
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
            return (res * 2.75) / 1000;
        }
        static void calculate_profit(AlgorithmTesting.AssignedTriple t)
        {
            double profit = 0.0;           
            double speed = 30.0; // km per hour
            if (t.type_of_delivery == 1)
            {
                // self source
                profit = 10;
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
                                   + GetDistance(t.source.y, t.source.x, t.crowdshipper.y_dest, t.crowdshipper.x_dest)
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
            if (n == 0 || n == 3)
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
                                   + GetDistance(assigned.source.y, assigned.source.x, assigned.crowdshipper.y_dest, assigned.crowdshipper.x_dest)                                   
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
                    AlgorithmTesting.Node temp = new AlgorithmTesting.Node(AlgorithmTesting.Program.supplier_nodes[k].x, AlgorithmTesting.Program.supplier_nodes[k].y);
                    supplies.Add(temp);
                }                
                // Requests 
                List<AlgorithmTesting.Node> requests = new List<AlgorithmTesting.Node>();
                for (int k = 0; k < AlgorithmTesting.Program.demander_nodes.Count; k++)
                {
                    AlgorithmTesting.Node temp = new AlgorithmTesting.Node(AlgorithmTesting.Program.demander_nodes[k].x, AlgorithmTesting.Program.demander_nodes[k].y);
                    requests.Add(temp);
                }
                // Crowdshippers 
                List<AlgorithmTesting.Trip> crowdshippers = new List<AlgorithmTesting.Trip>();
                for (int k = 0; k < AlgorithmTesting.Program.crowdshipper_nodes.Count; k++)
                {
                    AlgorithmTesting.Trip temp = new AlgorithmTesting.Trip(AlgorithmTesting.Program.crowdshipper_nodes[k].x_src,
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
                    AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(sup, req, null);
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

                    AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(sup, null, crowd);
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

                    AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(null, req, crowd);
                    req_crowd.Add(temp);
                }

                //Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", supplies.Count, requests.Count ,crowdshippers.Count, sup_req.Count, sup_crowd.Count, req_crowd.Count);

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
                    if (sup_req.Count == 0 && sup_crowd.Count == 0 && req_crowd.Count == 0)
                    {
                        AlgorithmTesting.AssignedTriple selected = new AlgorithmTesting.AssignedTriple(supplies[0], requests[0], null);
                        selected.type_of_delivery = 4;
                        calculate_profit(selected);
                        feasibleSolution.Add(selected);
                        
                        remove_from_list_of_nodes(supplies, selected.source);
                        remove_from_list_of_nodes(requests, selected.destination);
                        //Console.WriteLine("sup req {0}, {1}, {2}, {3}, {4}, {5}", supplies.Count, requests.Count, crowdshippers.Count, sup_req.Count, sup_crowd.Count, req_crowd.Count);

                    }
                    // Randomly get a number between 0, 1, 2
                    // Self_Sourcing, Home_Delivery, Neighborhood_Delivery
                    int randomNum = random.Next(3);
                    //Console.WriteLine(randomNum);
                    if (randomNum == 0 && sup_req.Count > 0)
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
                        AlgorithmTesting.Node src = new AlgorithmTesting.Node(selected.source.x, selected.source.y);
                        AlgorithmTesting.Node dst = new AlgorithmTesting.Node(selected.destination.x, selected.destination.y);                        
                        AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(src, dst, null);
                        remove_from_list_of_triples(sup_crowd, temp, 3);                                    
                        remove_from_list_of_triples(req_crowd, temp, 2);

                        //Console.WriteLine("sup req {0}, {1}, {2}, {3}, {4}, {5}", supplies.Count, requests.Count, crowdshippers.Count, sup_req.Count, sup_crowd.Count, req_crowd.Count);
                    }
                    if (randomNum == 1 && req_crowd.Count > 0)
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
                        //req_crowd.Remove(selected_req_crowd);                                            
                        remove_from_list_of_nodes(supplies, selected_req_crowd.source);
                        remove_from_list_of_nodes(requests, selected_req_crowd.destination);
                        remove_from_list_of_trips(crowdshippers, selected_req_crowd.crowdshipper);
                        // also remove from sup_crowd    
                        AlgorithmTesting.Node src = new AlgorithmTesting.Node(selected_req_crowd.source.x, selected_req_crowd.source.y);
                        AlgorithmTesting.Node dst = new AlgorithmTesting.Node(selected_req_crowd.destination.x, selected_req_crowd.destination.y);
                        AlgorithmTesting.Trip trip = new AlgorithmTesting.Trip(selected_req_crowd.crowdshipper.x_src, selected_req_crowd.crowdshipper.y_src, selected_req_crowd.crowdshipper.x_dest, selected_req_crowd.crowdshipper.y_dest);
                        AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(src, dst, trip);
                        remove_from_list_of_triples(sup_crowd, temp, 1);
                        remove_from_list_of_triples(sup_crowd, temp, 3);
                        remove_from_list_of_triples(req_crowd, temp, 1);
                        remove_from_list_of_triples(req_crowd, temp, 2);
                        remove_from_list_of_triples(sup_req, temp, 3);
                        remove_from_list_of_triples(sup_req, temp, 2);
                        //Console.WriteLine("req crowd {0}, {1}, {2}, {3}, {4}, {5}", supplies.Count, requests.Count, crowdshippers.Count, sup_req.Count, sup_crowd.Count, req_crowd.Count);
                    }

                    if (randomNum == 2 && sup_crowd.Count > 0)
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
                        AlgorithmTesting.Node src = new AlgorithmTesting.Node(selected_sup_crowd.source.x, selected_sup_crowd.source.y);
                        AlgorithmTesting.Node dst = new AlgorithmTesting.Node(selected_sup_crowd.destination.x, selected_sup_crowd.destination.y);
                        AlgorithmTesting.Trip trip = new AlgorithmTesting.Trip(selected_sup_crowd.crowdshipper.x_src, selected_sup_crowd.crowdshipper.y_src, selected_sup_crowd.crowdshipper.x_dest, selected_sup_crowd.crowdshipper.y_dest);
                        AlgorithmTesting.AssignedTriple temp = new AlgorithmTesting.AssignedTriple(src, dst, trip);
                        
                        remove_from_list_of_triples(req_crowd, temp, 1);
                        remove_from_list_of_triples(req_crowd, temp, 2);
                        remove_from_list_of_triples(sup_crowd, temp, 1);
                        remove_from_list_of_triples(sup_crowd, temp, 3);
                        remove_from_list_of_triples(sup_req, temp, 3);
                        remove_from_list_of_triples(sup_req, temp, 2);
                        //Console.WriteLine("sup crowd {0}, {1}, {2}, {3}, {4}, {5}", supplies.Count, requests.Count, crowdshippers.Count, sup_req.Count, sup_crowd.Count, req_crowd.Count);
                    }

                    j++;             

                }
                // Add the i-th list to the population list
                //Console.WriteLine(feasibleSolution.Count);
                initial_population.Add(feasibleSolution);
                //print_list(feasibleSolution, 200);
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

            //Console.WriteLine("\n------------\n New Gen \n");
            //for (int x = 0; x < n; x++)
            //{
            //    Console.WriteLine("index: {0}, fitness: {1}", index_keeper[x], fitness_values[x]);
            //}

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

                    for (int k = 0; k < 10; k++)
                    {
                        int rand1 = random.Next(members[j].Count);
                        int rand2 = random.Next(members[j].Count);

                        int rand3 = random.Next(4);
                        // 0: change the src
                        // 1: change the dest
                        // 2: change the crowdshipper
                        // 3: change type of delivery

                        while (rand1 == rand2)
                        {
                            rand2 = random.Next(members[j].Count);
                        }

                        var assignment1 = members[j][rand1];
                        var assignment2 = members[j][rand2];

                        if (rand3 == 0)
                        { // swap suppliers
                            AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment1.destination, assignment1.crowdshipper);
                            AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment2.destination, assignment2.crowdshipper);
                            if (check_feasibility(child1, assignment1.type_of_delivery - 1) && check_feasibility(child2, assignment2.type_of_delivery - 1))
                            {
                                calculate_profit(child1);
                                calculate_profit(child2);
                                members[j][rand1] = child1;                                
                                members[j][rand2] = child2;                                
                            }
                        }

                        else if (rand3 == 1)
                        {
                            // swap requests
                            AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment2.destination, assignment1.crowdshipper);
                            AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment1.destination, assignment2.crowdshipper);
                            if (check_feasibility(child1, assignment1.type_of_delivery - 1) && check_feasibility(child2, assignment2.type_of_delivery - 1))
                            {
                                calculate_profit(child1);
                                calculate_profit(child2);
                                members[j][rand1] = child1;
                                members[j][rand2] = child2;
                            }
                        }
                        else if (rand3 == 2)
                        {
                            // swap crowdshippers
                            AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment1.destination, assignment2.crowdshipper);
                            AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment2.destination, assignment1.crowdshipper);
                            if (check_feasibility(child1, assignment2.type_of_delivery - 1) && check_feasibility(child2, assignment1.type_of_delivery - 1))
                            {
                                calculate_profit(child1);
                                calculate_profit(child2);
                                members[j][rand1] = child1;
                                members[j][rand2] = child2;
                            }

                        }
                        else if (rand3 == 3)
                        {
                            // change types
                            // a little different from the previous ones
                            AlgorithmTesting.AssignedTriple child1 = new AlgorithmTesting.AssignedTriple(assignment1.source, assignment1.destination, assignment1.crowdshipper);
                            int flag = assignment1.type_of_delivery;
                            calculate_profit(assignment1);
                            double profit = assignment1.profit;       
                            if (assignment1.type_of_delivery == 4 && check_feasibility(child1, 0))
                            {
                                calculate_profit(child1);                                
                                members[j][rand1] = child1;
                            }  
                            else if (assignment1.type_of_delivery == 2 || assignment1.type_of_delivery == 3)
                            {
                                for(int p=0; p<3; p++)
                                {
                                    if(check_feasibility(child1, p))
                                    {
                                        calculate_profit(child1);
                                        double profit_new = child1.profit;
                                        if (profit_new > profit)
                                        {
                                            profit = profit_new;
                                            flag = p;
                                        }
                                    }
                                }
                                check_feasibility(child1, flag);
                                calculate_profit(child1);                                
                                members[j][rand1] = child1;
                            }

                            AlgorithmTesting.AssignedTriple child2 = new AlgorithmTesting.AssignedTriple(assignment2.source, assignment2.destination, assignment2.crowdshipper);
                            int flag2 = assignment2.type_of_delivery;
                            calculate_profit(assignment2);
                            double profit2 = assignment2.profit;
                            if (assignment2.type_of_delivery == 4 && check_feasibility(child2, 0))
                            {                                
                                calculate_profit(child2);
                                members[j][rand2] = child2;
                            }
                            else if (assignment2.type_of_delivery == 2 || assignment2.type_of_delivery == 3)
                            {
                                for (int p = 0; p < 3; p++)
                                {
                                    if (check_feasibility(child2, p))
                                    {
                                        calculate_profit(child2);
                                        double profit_new = child2.profit;
                                        if (profit_new > profit2)
                                        {
                                            profit2 = profit_new;
                                            flag2 = p;
                                        }
                                    }
                                }
                                check_feasibility(child2, flag2);                                
                                calculate_profit(child2);
                                members[j][rand2] = child2;
                            }
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



        public void Run(int index, int size)
        {            
            int population_size = 300;
            int number_of_iterations = 500;            

            Generate_Initial_Population(population_size);
            //Console.WriteLine(initial_population[0].Count);
            // create as many possible assignments as the size given

            for(int i=0; i<number_of_iterations; i++)
            {
                Calculate_Fitness();
                // inside the initial pop and assigned object
                // calculate the value of fitness

                Write_profit_iteration_file(index, i, size);
                Write_type_iteration(index, i, size);

                Select_Parents();
                // select as many chromosome as the size of initial pop

                Create_Next_Generation();
                // crossover and mutation baby B)
            }
            Console.WriteLine("Total profit: {0}\n", fitness_values[0]);
            write_results_in_file(index, size);
        }


    }
}

