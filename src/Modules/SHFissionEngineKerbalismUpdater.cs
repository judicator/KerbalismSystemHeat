using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatFissionEngineKerbalismUpdater : PartModule
	{
		public static string brokerName = "SHFissionEngine";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismSystemHeat_Brokers_FissionEngine");

		// This should correspond to the related ModuleSystemHeatFissionReactor
		[KSPField(isPersistant = true)]
		public string engineModuleID;

		[KSPField(isPersistant = true)]
		public float MaxECGeneration = 0f;
		[KSPField(isPersistant = true)]
		public float MinECGeneration = 0f;

		protected static string engineModuleName = "ModuleSystemHeatFissionEngine";
		protected ModuleSystemHeatFissionReactor engineModule;

		protected bool resourcesListParsed = false;
		protected List<ResourceRatio> inputs;
		protected List<ResourceRatio> outputs;

		public virtual void Start()
		{
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
			{
				engineModule = FindEngineModule(part, engineModuleID);
				if (inputs == null || inputs.Count == 0)
				{
					ConfigNode node = ModuleUtils.GetModuleConfigNode(part, moduleName);
					if (node != null)
					{
						OnLoad(node);
					}
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			ParseResourcesList(part);
		}

		public virtual void FixedUpdate()
		{
			if (!engineModule && HighLogic.LoadedSceneIsFlight)
			{
				MaxECGeneration = engineModule.ElectricalGeneration.Evaluate(100f) * engineModule.CoreIntegrity / 100f;
				MinECGeneration = engineModule.ElectricalGeneration.Evaluate(engineModule.MinimumThrottle);
			}
		}

		// Fetch input/output resources list from reactor ConfigNode
		protected void ParseResourcesList(Part part)
		{
			if (!resourcesListParsed)
			{
				ConfigNode node = ModuleUtils.GetModuleConfigNode(part, engineModuleName);
				if (node != null)
				{
					// Load resource nodes
					ConfigNode[] inNodes = node.GetNodes("INPUT_RESOURCE");
					inputs = new List<ResourceRatio>();
					for (int i = 0; i < inNodes.Length; i++)
					{
						ResourceRatio p = new ResourceRatio();
						p.Load(inNodes[i]);
						inputs.Add(p);
					}
					ConfigNode[] outNodes = node.GetNodes("OUTPUT_RESOURCE");
					outputs = new List<ResourceRatio>();
					for (int i = 0; i < outNodes.Length; i++)
					{
						ResourceRatio p = new ResourceRatio();
						p.Load(outNodes[i]);
						outputs.Add(p);
					}
				}
				resourcesListParsed = true;
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (engineModule != null)
			{
				string title = brokerTitle;

				float curECGeneration = engineModule.ElectricalGeneration.Evaluate(engineModule.CurrentReactorThrottle);
				if (curECGeneration > 0)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", curECGeneration));
				}

				float fuelThrottle = engineModule.CurrentReactorThrottle / 100f;
				if (fuelThrottle > 0)
				{
					ParseResourcesList(part);
					foreach (ResourceRatio ratio in inputs)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, -fuelThrottle * ratio.Ratio));
					}
					foreach (ResourceRatio ratio in outputs)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, fuelThrottle * ratio.Ratio));
					}
				}
				return title;
			}
			return "ERR: no engine";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactor = FindEngineSnapshot(part_snapshot);
			if (reactor != null)
			{
				string title = brokerTitle;
				if (Lib.Proto.GetBool(reactor, "Enabled") && Lib.Proto.GetBool(reactor, "GeneratesElectricity"))
				{
					float maxGeneration = Lib.Proto.GetFloat(module_snapshot, "MaxECGeneration");
					float minGeneration = Lib.Proto.GetFloat(module_snapshot, "MinECGeneration");
					float curECGeneration = Lib.Proto.GetFloat(reactor, "CurrentElectricalGeneration");
					float fuelThrottle = Lib.Proto.GetFloat(reactor, "CurrentReactorThrottle") / 100f;
					double ecToGenerate = curECGeneration;
					VesselResources resources = KERBALISM.ResourceCache.Get(v);
					if (!Lib.Proto.GetBool(reactor, "ManualControl") && maxGeneration > 0f)
					{
						ecToGenerate = resources.GetResource(v, "ElectricCharge").Capacity - resources.GetResource(v, "ElectricCharge").Amount;
						ecToGenerate -= resources.GetResource(v, "ElectricCharge").Deferred;
						if (elapsed_s > 0)
						{
							ecToGenerate /= elapsed_s;
						}
						if (ecToGenerate < minGeneration)
						{
							ecToGenerate = minGeneration;
						}
						else
						{
							ecToGenerate = Lib.Clamp(ecToGenerate, (double)minGeneration, ecToGenerate);
						}
						if (ecToGenerate != curECGeneration)
						{
							Lib.Proto.Set(reactor, "CurrentElectricalGeneration", (float)ecToGenerate);
						}
						double ff = ecToGenerate / maxGeneration;
						if (ff != fuelThrottle)
						{
							fuelThrottle = (float)ff;
							Lib.Proto.Set(reactor, "CurrentReactorThrottle", fuelThrottle * 100f);
						}
					}
					// Resources generation/consumption according to reactor throttle parameter
					if (ecToGenerate > 0)
					{
						if (!(proto_part_module as SystemHeatFissionEngineKerbalismUpdater).resourcesListParsed)
						{
							(proto_part_module as SystemHeatFissionEngineKerbalismUpdater).ParseResourcesList(proto_part);
						}
						ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(brokerName, KERBALISM.ResourceBroker.BrokerCategory.Converter, brokerTitle));
						bool NeedToStopReactor = false;
						foreach (ResourceRatio ir in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).inputs)
						{
							recipe.AddInput(ir.ResourceName, ir.Ratio * fuelThrottle * elapsed_s);
							if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
							{
								// Input resource amount is zero - stop reactor
								NeedToStopReactor = true;
							}
						}
						foreach (ResourceRatio or in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).outputs)
						{
							recipe.AddOutput(or.ResourceName, or.Ratio * fuelThrottle * elapsed_s, dump: false);
							if (1 - resources.GetResource(v, or.ResourceName).Level < double.Epsilon)
							{
								// Output resource is at full capacity
								NeedToStopReactor = true;
								Message.Post(
									Severity.warning,
									Localizer.Format(("#LOC_KerbalismSystemHeat_ReactorOutputResourceFull"), or.ResourceName, v.GetDisplayName(), part_snapshot.partName)
								);
							}
						}
						recipe.AddOutput("ElectricCharge", ecToGenerate * elapsed_s, dump: true);
						resources.AddRecipe(recipe);

						// Disable reactor
						if (NeedToStopReactor)
						{
							Lib.Proto.Set(reactor, "Enabled", false);
						}
					}
				}
				// Set LastUpdate to current time - this is done to prevent double resources consumption
				// Background resource consumption is already handled earlier in this method,
				// no need to do it again on craft activation in ModuleSystemHeatFissionReactor.DoCatchup()
				Lib.Proto.Set(reactor, "LastUpdateTime", Planetarium.GetUniversalTime());
				return title;
			}
			return "ERR: no engine";
		}

		// Find associated Engine module
		public ModuleSystemHeatFissionEngine FindEngineModule(Part part, string moduleName)
		{
			ModuleSystemHeatFissionEngine engine = part.GetComponents<ModuleSystemHeatFissionEngine>().ToList().Find(x => x.moduleID == moduleName);

			if (engine == null)
			{
				KSHUtils.LogError($"[{part}] No ModuleSystemHeatFissionEngine named {moduleName} was found, using first instance");
				engineModule = part.GetComponents<ModuleSystemHeatFissionEngine>().ToList().First();
			}
			if (engine == null)
			{
				KSHUtils.LogError($"[{part}] No ModuleSystemHeatFissionEngine was found.");
			}
			return engine;
		}

		// Find PartModule snapshot (used for unloaded vessels as they only have Modules snapshots)
		protected static ProtoPartModuleSnapshot FindEngineSnapshot(ProtoPartSnapshot p)
		{
			ProtoPartModuleSnapshot m = null;
			for (int i = 0; i < p.modules.Count; i++)
			{
				if (p.modules[i].moduleName == engineModuleName)
				{
					m = p.modules[i];
					break;
				}
			}
			if (m == null)
			{
				KSHUtils.LogError($" Part [{p.partName}] No {engineModuleName} was found in part snapshot.");
			}
			return m;
		}
	}
}
