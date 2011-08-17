﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vixen.Sys;

namespace Vixen.Module.ModuleTemplate {
	class ModuleTemplateModuleRepository : IModuleRepository<IModuleTemplateModuleInstance> {
		private Dictionary<Guid, IModuleTemplateModuleInstance> _instances = new Dictionary<Guid, IModuleTemplateModuleInstance>();

		public void Add(Guid id) {
			// Create an instance.
			IModuleTemplateModuleInstance instance = Modules.GetById(id) as IModuleTemplateModuleInstance;
			// Load data from user data.
			VixenSystem.ModuleData.GetModuleTypeData(instance);
			//ModuleTemplateModuleManagement manager = Modules.GetModuleManager<IModuleTemplateModuleInstance, ModuleTemplateModuleManagement>();
			//manager.LoadTemplateData(instance);
			// Reference the instance.
			_instances[id] = instance;
		}

		public IModuleTemplateModuleInstance Get(Guid id) {
			IModuleTemplateModuleInstance instance = null;
			_instances.TryGetValue(id, out instance);
			return instance;
		}

		public IModuleTemplateModuleInstance[] GetAll() {
			return _instances.Values.ToArray();
		}

		public void Remove(Guid id) {
			_instances.Remove(id);
		}

		object IModuleRepository.Get(Guid id) {
			return Get(id);
		}

		object[] IModuleRepository.GetAll() {
			return GetAll();
		}
	}
}