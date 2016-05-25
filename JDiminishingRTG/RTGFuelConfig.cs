/*
 * Copyright KwirkyJ (Jake Smith)
 * 
 * Available for used under the LGPL v3 license.
 * You may use, recompile, modify, and redistribute with this license
 * and attribution.
 */
using System;
using UnityEngine;

namespace JDiminishingRTG
{
    public class RTGFuelConfig
    {
        public string resourceName { get; private set; }
        public float halflife { get; private set; }
        public float pep { get; private set; }
        public float density { get; private set; }

        public RTGFuelConfig (ConfigNode node)
        {
            this.resourceName = "";
            this.halflife = 100;
            this.pep = 0;
            this.density = 0;

            if (node.HasValue ("name")) 
            {
                bool matched = false;
                this.resourceName = node.GetValue ("name");
                foreach (ConfigNode r in GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION")) 
                {
                    if (r.GetValue ("name") == this.resourceName) 
                    {
                        matched = true;
                        this.density = float.Parse (r.GetValue ("density"));
                        break;
                    }
                }
                if (!matched) 
                {
                    // desired behavior?
                    throw new MissingFieldException ("resource '" + this.resourceName + "' not matched in gamedatabase");
                }
            } 
            else 
            {
                throw new FormatException ("RTGfuelconfig lacking 'name' node");
            }

            if (node.HasValue ("halflife")) 
            {
                this.halflife = float.Parse (node.GetValue ("halflife"));
            } 
            else 
            {
                // throw new FormatException ("RTGfuelconfig lacking 'halflife' node");
            }

            if (node.HasValue ("pep")) 
            {
                this.pep = 1000F * float.Parse (node.GetValue ("pep"));
            } 
            else 
            {
                // throw new FormatException ("RTGfuelconfig lacking 'pep' node");
            }
        }
    }
}

