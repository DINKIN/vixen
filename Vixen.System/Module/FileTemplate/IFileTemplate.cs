﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vixen.Module.FileTemplate {
	public interface IFileTemplate {
		void Project(object target);
		void Setup();
	}
}