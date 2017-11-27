﻿using CSharpTranspiler.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CSharpTranspiler
{
	public enum ProjectTypes
	{
		Exe,
		Dll
	}

	public class Project
	{
		public Microsoft.CodeAnalysis.Project project;

		public ProjectTypes type;
		public bool isReleaseBuild;
		public string filename, assemblyName;
		public List<string> references;

		public List<ObjectBase> allObjects;
		public List<ClassObject> classObjects;
		public List<StructObject> structObjects;
		public List<InterfaceObject> interfaceObjects;
		public List<EnumObject> enumObjects;

		public Project(string filename)
		{
			this.filename = filename;
		}

		public async Task Parse(Microsoft.CodeAnalysis.Project project)
		{
			// init main objects
			this.project = project;
			assemblyName = project.AssemblyName;
			allObjects = new List<ObjectBase>();
			classObjects = new List<ClassObject>();
			structObjects = new List<StructObject>();
			interfaceObjects = new List<InterfaceObject>();
			enumObjects = new List<EnumObject>();

			// validate compiler options
			var parseOptions = (CSharpParseOptions)project.ParseOptions;
			if (parseOptions.LanguageVersion != LanguageVersion.CSharp3) throw new Exception("Project lang version must be 3.0: " + project.FilePath);

			var compilationOptions = project.CompilationOptions;
			if (compilationOptions.Platform != Platform.AnyCpu) throw new Exception("Project platform must be AnyCpu: " + project.FilePath);
			
			// get project type
			var kind = compilationOptions.OutputKind;
			if (kind == OutputKind.DynamicallyLinkedLibrary) type = ProjectTypes.Dll;
			else if (kind == OutputKind.ConsoleApplication || kind == OutputKind.WindowsApplication) type = ProjectTypes.Exe;
			else throw new Exception("Unsuported project kind: " + project.FilePath);

			// check optimization level
			isReleaseBuild = compilationOptions.OptimizationLevel == OptimizationLevel.Release;

			// gather references
			references = new List<string>();
			var sln = project.Solution;
			foreach (var reference in project.AllProjectReferences)
			{
				var p = sln.GetProject(reference.ProjectId);
				references.Add(p.AssemblyName);
			}

			// parse syntax tree
			var compilation = await project.GetCompilationAsync();
			foreach (var doc in project.Documents)
			{
				var syntaxTree = await doc.GetSyntaxTreeAsync() as CSharpSyntaxTree;
				if (syntaxTree == null) throw new Exception("Not a C# file: " + doc.FilePath);
				var semanticModel = await doc.GetSemanticModelAsync();
				AddObjects(syntaxTree.GetRoot().ChildNodes(), syntaxTree, semanticModel);
			}

			// all objects to all list
			allObjects.AddRange(enumObjects);
			allObjects.AddRange(interfaceObjects);
			allObjects.AddRange(structObjects);
			allObjects.AddRange(classObjects);
		}

		private bool DoesPartialObjectExist(string fullName, out ObjectBase objBase)
		{
			objBase = classObjects.FirstOrDefault(x => x.fullName == fullName);
			if (objBase != null) return true;

			objBase = interfaceObjects.FirstOrDefault(x => x.fullName == fullName);
			if (objBase != null) return true;

			objBase = structObjects.FirstOrDefault(x => x.fullName == fullName);
			if (objBase != null) return true;

			objBase = null;
			return false;
		}

		private delegate void AddObjectCallbackMethod(string fullName);
		private void AddObject(TypeDeclarationSyntax node, CSharpSyntaxTree syntaxTree, SemanticModel semanticModel, SyntaxTokenList modifiers, AddObjectCallbackMethod callback)
		{
			string fullName = semanticModel.GetDeclaredSymbol(node).ToString();
			bool addNew = false;
			if (Tools.HasKind(modifiers, SyntaxKind.PartialKeyword))
			{
				ObjectBase obj;
				if (DoesPartialObjectExist(fullName, out obj)) obj.MergePartial(node, semanticModel);
				else addNew = true;
			}
			else
			{
				addNew = true;
			}
					
			if (addNew) callback?.Invoke(fullName);
			AddObjects(node.ChildNodes(), syntaxTree, semanticModel);
		}

		private void AddObjects(IEnumerable<SyntaxNode> syntaxNodes, CSharpSyntaxTree syntaxTree, SemanticModel semanticModel)
		{
			foreach (var node in syntaxNodes)
			{
				var type = node.GetType();
				if (type == typeof(NamespaceDeclarationSyntax))
				{
					var namespaceNode = (NamespaceDeclarationSyntax)node;
					AddObjects(namespaceNode.ChildNodes(), syntaxTree, semanticModel);
				}
				else if (type == typeof(ClassDeclarationSyntax))
				{
					var classNode = (ClassDeclarationSyntax)node;
					void AddObjectCallback(string fullName)
					{
						string name = classNode.Identifier.ValueText;
						classObjects.Add(new ClassObject(name, fullName, classNode, semanticModel));
					}

					AddObject(classNode, syntaxTree, semanticModel, classNode.Modifiers, AddObjectCallback);
				}
				else if (type == typeof(StructDeclarationSyntax))
				{
					var structNode = (StructDeclarationSyntax)node;
					void AddObjectCallback(string fullName)
					{
						string name = structNode.Identifier.ValueText;
						structObjects.Add(new StructObject(name, fullName, structNode, semanticModel));
					}

					AddObject(structNode, syntaxTree, semanticModel, structNode.Modifiers, AddObjectCallback);
				}
				else if (type == typeof(InterfaceDeclarationSyntax))
				{
					var interfaceNode = (InterfaceDeclarationSyntax)node;
					void AddObjectCallback(string fullName)
					{
						string name = interfaceNode.Identifier.ValueText;
						interfaceObjects.Add(new InterfaceObject(name, fullName, interfaceNode, semanticModel));
					}

					AddObject(interfaceNode, syntaxTree, semanticModel, interfaceNode.Modifiers, AddObjectCallback);
				}
				else if (type == typeof(EnumDeclarationSyntax))
				{
					var enumNode = (EnumDeclarationSyntax)node;
					string name = enumNode.Identifier.ValueText;
					string fullName = semanticModel.GetDeclaredSymbol(node).ToString();
					enumObjects.Add(new EnumObject(name, fullName, enumNode, semanticModel));
				}
			}
		}
	}
}
