# VMAT-TBI-CSI-TMLI Auto-Planner

This repository contains the source code for a suite of tools designed to automate the treatment planning process for VMAT-based Craniospinal Irradiation (CSI), Total Body Irradiation (TBI), and Total Marrow and Lymphoid Irradiation (TMLI). The application is developed in C# using WPF for the user interface and is intended for use in a clinical radiation oncology setting.

## Authors
- Primary contributors and authors:
	- Eric Simiele: primary developer and maintainer of code
	- Ignacio Romero: developer and tester
	- Nataliya Kovalchuk: tester and project PI

## Updates
See [Changes log]()

## Key Features

* **Automated Planning:** Scripts and workflows to automate plan generation for CSI, TBI, and TMLI.
* **Optimization Loop:** An iterative process to refine treatment plan parameters to meet clinical objectives.
* **Modular Design:** The solution is broken down into several projects, each handling a specific part of the workflow:
  * `CSIAutoPlanner`: Handles the specifics of Craniospinal Irradiation planning.
  * `TBIAutoPlanner`: Manages Total Body Irradiation workflows.
  * `TMLIAutoPlanner`: Contains the logic for Total Marrow and Lymphoid Irradiation.
  * `AutoPlannerOptimizationLoop`: A dedicated tool for running plan optimizations.
  * `AutoPlannerHelpers`: Shared library with common code and utilities used across modules.
  * `ImportListener`: Service that listens for new patient data imports.
* **DICOM Integration:** Uses libraries like `EvilDICOM` and `SimpleITK` to handle medical imaging data.
* **Configuration Templates:** INI-based configuration files for each modality to standardize planning parameters.
* **Plan Templates:** Predefined templates for different prescription doses (e.g., TMLI_2Gy.ini, TMLI_12Gy.ini, TMLI_20Gy.ini).

## Project Structure

The main application logic is contained within the `VMATTBICSITMLIAutoPlanner` directory, which is a Visual Studio Solution (`.sln`).

* `/VMATTBICSITMLIAutoPlanner`: Root directory for the Visual Studio solution.
  * `/AutoPlannerLauncher`: Main entry point and launcher for the various tools.
  * `/CSIAutoPlanner`, `/TBIAutoPlanner`, `/TMLIAutoPlanner`: Individual applications for each treatment modality.
  * `/AutoPlannerOptimizationLoop`: Core optimization engine.
  * `/AutoPlannerHelpers`: Shared library with common code and utilities.
  * `/ImportListener`: Patient data import listener service.
  * `/PlanTemplates`: Contains modality-specific plan templates.
  * `/Configuration`: INI configuration files for each modality.
  * `/AutoPlannerOptimizationLoopTests`: Unit tests for optimization loop components.


## Initial public release of code v1.0
- The purpose of this code is to automate as much of the treatment planning process as possible for VMAT TBI, CSI, and TMLI following the planning techniques used at Stanford University
- Code is now available to public following clinical release at Stanford
- Feel free to download and use the code
- No example patients are provided with the code
	- Wanted to avoid dealing with any privacy/HIPAA issues
- The solution file is located under VMATTBICSITMLIAutoPlanner/VMATTBICSITMLIAutoPlanner.sln
- This code has been developed over the past year through a Stanford Clinical Innovation Fund grant

## Getting Started

### Prerequisites

* Visual Studio 2019 or later
* .NET Framework 4.6.1 or higher
* Access to a clinical treatment planning system (for data and validation)

## Install and run guide
### Install and build
- Clone the repository to your local computer
- Open the solution file in visual studio
- Fixes any references to the Varian DLL files for the following projects:
	- ImportListener
	- CSIAutoPlanner
	- TBIAutoPlanner
    - TMLIAutoPlanner
	- AutoPlannerHelpers
	- AutoPlannerOptimizationLoop
    - AutoPlannerLauncher
- The code was compiled using the v15.6 ESAPI libraries
- Ignore any warnings from the test projects
- Under the CSIAutoPlanner, TBIAutoPlanner, and TMLIAutoPlanner projects, in the Configuration folders, open the .ini configuration files and update the file paths to the desired locations on the computer/network
	- documentation
	- image export
	- Aria DB daemon, VMS File Daemon, local Daemon information (if you want automated import/export of CT and RT Struct dicom data)
	- RT structure set import file location
- At the top of Visual Studio select --> build --> rebuild solution
- Resolve any build errors

### Troubleshooting
- Again, don't care about build failures for test projects
- A likely failure is the varian ESAPI dlls not being found/correctly referenced
	- be sure to update them to **YOUR VERSION OF ECLIPSE/ESAPI** for all projects listed in the installation section
- Another failure is in nuget package restoration. This project uses two main nuget packages: simpleprogresswindow and EvilDicom
- Info on the two packages can be found here:
	- Github: [SimpleProgressWindow](https://github.com/esimiele/SimpleProgressWindow) Nuget: [SimpleProgressWindow](https://www.codecademy.com/resources/docs/markdown/links)
	- [EvilDicom](https://github.com/rexcardan/Evil-DICOM) 

### Build files and testing
- All files will be built in the top parent directory under /bin
- Included in this folder are the configuration files (placed in a created folder under /bin/configuration) and plan template files (/bin/templates/\<plan type\>)
	- All autoplanning scripts have been built around the concept of plan templates
	- These plan template files are read upon launch and are available to the user for selection for planning
		- They contain the relevant information regarding targets, rings, optimization structures, optimization constraints, etc.
- Upon successful build of all projects, at the top of Visual Studio next to the build configuration drop downs, select the CSIAutoPlanner project in the drop down (i.e., which project to launch in debug mode)
- Hit Start, there should be one error message regarding no connection to aria. Pay attention to any other error messages, particularly any messages regarding not being able to find folders
- Once the gui pops up, switch to the Script configuration tab and review the settings to ensure they match what you changed in the .ini files previously

### Run
- The scripts can be run either through citrix or as stand-alone applications on a thick-client
- To run through citrix:
	- Copy the bin/ folder and all of its contents to a network drive that citrix can access
	- Open a patient structure set in Eclipse, select tools --> scripts --> change folder --> select folder --> navigate to the bin/ directory --> hit ok
		- A script should show up in the scripts window called *LaunchAutoPlanner.cs* select this script and hit run
		- You will be presented with a small window asking you to choose between VMAT TBI, VMAT CSI, or VMAT TMLI. Select VMAT CSI
		- The VMAT CSI UI should pop up. Repeat for other two options to ensure everything is wired up correctly
- To run as a stand-alone application:
	- Copy the bin/ folder and all of its contents to the desktop of a thick client
		- Open the bin/ folder and double click on the CSIAutoPlanner.exe file
			- You will be prompted to enter a patient MRN and a warning message should pop up saying that you need to select a structure set in the UI

### Approvals
- Please test the code on a t-box prior to approving in your clinical system
	- **I'm not responsible if the code is not configured correctly for your system and it ends up causing problems**
- Once you have configured the code correctly and tested it and are ready to move to the clinical system, you will need to approve the following files under script approvals:
	- ImportListener
	- CSIAutoPlanner
	- TBIAutoPlanner
    - TMLIAutoPlanner
	- AutoPlannerHelpers
	- AutoPlannerOptimizationLoop

## Contributing
- The authors welcome contributions, suggestions, issues, etc.
- For contributions, fork the code, make your changes and open a pull request with a short description of your changes
	- I will review it and determine if it should be incorporated into the code
- For all other items:
	- Feel free to open an issue for problems with the code or feature requests
	- I monitor it fairly regularly so I should get back to you in a week or so