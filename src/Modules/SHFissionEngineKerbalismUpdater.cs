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

		[KSPField(isPersistant = true)]
		public bool FirstLoad = true;

		// This should correspond to the related ModuleSystemHeatFissionReactor
		[KSPField(isPersistant = true)]
		public string engineModuleID;

		[KSPField(isPersistant = true)]
		public float MaxECGeneration = 0f;
		[KSPField(isPersistant = true)]
		public float MinThrottle = 0.25f;
		[KSPField(isPersistant = true)]
		public float MaxThrottle = 1.0f;
		[KSPField(isPersistant = true)]
		public bool GeneratesElectricity = true;

		protected static string engineModuleName = "ModuleSystemHeatFissionEngine";
		protected ModuleSystemHeatFissionReactor engineModule;

		protected bool resourcesListParsed = false;
		protected List<ResourceRatio> inputs;
		protected List<ResourceRatio> outputs;

		public virtual void Start()
		{
			if (Lib.IsFlight() || Lib.IsEditor())
			{
				if (engineModule == null)
				{
					engineModule = FindEngineModule(part, engineModuleID);
				}
				if (FirstLoad)
				{
					if (engineModule != null)
					{
						MaxECGeneration = engineModule.ElectricalGeneration.Evaluate(100f);
						MinThrottle = engineModule.MinimumThrottle / 100f;
						GeneratesElectricity = engineModule.GeneratesElectricity;
						//LastReactorState = engineModule.Enabled;
					}
					if (inputs == null || inputs.Count == 0)
					{
						ConfigNode node = ModuleUtils.GetModuleConfigNode(part, moduleName);
						if (node != null)
						{
							OnLoad(node);
						}
					}
					FirstLoad = false;
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
		}

		public virtual void FixedUpdate()
		{
			if (engineModule != null && Lib.IsFlight())
			{
				// Update MaxThrottle according to reactor CoreIntegrity
				MaxThrottle = engineModule.CoreIntegrity / 100f;
				if (MinThrottle > MaxThrottle)
				{
					MinThrottle = MaxThrottle;
				}
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
				return brokerTitle;
			}
			return "ERR: no engine";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactor = KSHUtils.FindPartModuleSnapshot(part_snapshot, engineModuleName);
			if (reactor != null)
			{
				if (Lib.Proto.GetBool(reactor, "Enabled") && Lib.Proto.GetBool(module_snapshot, "GeneratesElectricity"))
				{
					float curThrottle = Lib.Proto.GetFloat(reactor, "CurrentReactorThrottle") / 100f;
					float minThrottle = Lib.Proto.GetFloat(module_snapshot, "MinThrottle");
					float maxThrottle = Lib.Proto.GetFloat(module_snapshot, "MaxThrottle");
					float maxECGeneration = Lib.Proto.GetFloat(module_snapshot, "MaxECGeneration");
					bool needToStopReactor = false;
					if (maxECGeneration > 0)
					{
						VesselResources resources = KERBALISM.ResourceCache.Get(v);
						if (!(proto_part_module as SystemHeatFissionEngineKerbalismUpdater).resourcesListParsed)
						{
							(proto_part_module as SystemHeatFissionEngineKerbalismUpdater).ParseResourcesList(proto_part);
						}

						// Mininum reactor throttle
						// Some input/output resources will always be consumed/produced as long as minThrottle > 0
						if (minThrottle > 0)
						{
							ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
								brokerName,
								KERBALISM.ResourceBroker.BrokerCategory.Converter,
								brokerTitle));
							foreach (ResourceRatio ir in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).inputs)
							{
								recipe.AddInput(ir.ResourceName, ir.Ratio * minThrottle * elapsed_s);
								if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
								{
									// Input resource amount is zero - stop reactor
									needToStopReactor = true;
								}
							}
							foreach (ResourceRatio or in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).outputs)
							{
								recipe.AddOutput(or.ResourceName, or.Ratio * minThrottle * elapsed_s, dump: false);
								if (1 - resources.GetResource(v, or.ResourceName).Level < double.Epsilon)
								{
									// Output resource is at full capacity
									needToStopReactor = true;
									Message.Post(
										Severity.warning,
										Localizer.Format(
											"#LOC_KerbalismSystemHeat_ReactorOutputResourceFull",
											or.ResourceName,
											v.GetDisplayName(),
											part_snapshot.partName)
									);
								}
							}
							recipe.AddOutput("ElectricCharge", minThrottle * maxECGeneration * elapsed_s, dump: true);
							resources.AddRecipe(recipe);
						}

						if (!needToStopReactor)
						{
							if (!Lib.Proto.GetBool(reactor, "ManualControl"))
							{
								// Automatic reactor throttle mode
								curThrottle = maxThrottle;
							}
							curThrottle -= minThrottle;
							if (curThrottle > 0)
							{
								ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
									brokerName,
									KERBALISM.ResourceBroker.BrokerCategory.Converter,
									brokerTitle));
								foreach (ResourceRatio ir in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).inputs)
								{
									recipe.AddInput(ir.ResourceName, ir.Ratio * curThrottle * elapsed_s);
									if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
									{
										// Input resource amount is zero - stop reactor
										needToStopReactor = true;
									}
								}
								foreach (ResourceRatio or in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).outputs)
								{
									recipe.AddOutput(or.ResourceName, or.Ratio * curThrottle * elapsed_s, dump: false);
									if (1 - resources.GetResource(v, or.ResourceName).Level < double.Epsilon)
									{
										// Output resource is at full capacity
										needToStopReactor = true;
										Message.Post(
											Severity.warning,
											Localizer.Format(
												"#LOC_KerbalismSystemHeat_ReactorOutputResourceFull",
												or.ResourceName,
												v.GetDisplayName(),
												part_snapshot.partName)
										);
									}
								}
								recipe.AddOutput("ElectricCharge", curThrottle * maxECGeneration * elapsed_s, dump: false);
								resources.AddRecipe(recipe);
							}
						}
					}

					// Disable reactor
					if (needToStopReactor)
					{
						Lib.Proto.Set(reactor, "Enabled", false);
					}
				}
				// Prevent resource consumption in ModuleSystemHeatFissionReactor.DoCatchup()
				// by setting LastUpdate to current time
				Lib.Proto.Set(reactor, "LastUpdateTime", Planetarium.GetUniversalTime());
				return brokerTitle;
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
	}
}
