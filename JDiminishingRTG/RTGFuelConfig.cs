/*
 * Copyright Jake "KwirkyJ" Smith) <kwirkyj.smith0@gmail.com>
 * 
 * Available for use under the LGPL v3 license.
 */ 
using System;
using UnityEngine;

namespace JDiminishingRTG
{
    public class RTGFuelConfig
    {
        public string resourceName { get; private set; }
        public float  halflife     { get; private set; }
        public float  pep          { get; private set; }
        public float  density      { get; private set; }
        
        private static float DEFAULT_PEP      =   0;
        private static float DEFAULT_HALFLIFE = 100;

        public RTGFuelConfig (ConfigNode node)
        {
            if (!node.HasValue ("name")) {
                throw new FormatException ("RTGFuelConfig node lacking 'name'");
            }
            this.resourceName = node.GetValue ("name");
            ConfigNode resource_node = getResourceConfigNode(this.resourceName);
            if (resource_node == null) {
                throw new MissingFieldException ("resource '" + this.resourceName + "' not matched in gamedatabase");
            }
            this.density  = float.Parse (resource_node.GetValue ("density"));
            this.halflife = (node.HasValue ("halflife")) ? float.Parse (node.GetValue ("halflife"))
                                                         : DEFAULT_HALFLIFE;
            this.pep = (node.HasValue ("pep")) ? 1000F * float.Parse (node.GetValue ("pep"))
                                               : DEFAULT_PEP;
        }
        
        private static ConfigNode getResourceConfigNode(string name) {
            foreach (ConfigNode r in GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION")) {
                if (r.GetValue ("name") == name) {
                    return r;
                }
            }
            return null;
        }
    }
}

