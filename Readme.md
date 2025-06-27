# VMAT-TXI-CSI Auto-Planner

This repository contains the source code for a suite of tools designed to automate the treatment planning process for VMAT-based Craniospinal Irradiation (CSI), Total Body Irradiation (TBI), and Total Marrow and Lymphoid Irradiation (TMLI). The application is developed in C# using WPF for the user interface and is intended for use in a clinical radiation oncology setting.

## Overview

The primary goal of this project is to streamline and automate the complex process of generating treatment plans for advanced radiotherapy techniques. It provides a graphical user interface to guide the planner through the necessary steps, from data import to plan optimization.

## Key Features

*   **Automated Planning:** Scripts and workflows to automate plan generation for CSI, TBI, and TMLI.
*   **CT Stitching:** A utility to combine multiple CT image series, which is often required for long treatment fields like CSI.
*   **Optimization Loop:** An iterative process to refine treatment plan parameters to meet clinical objectives.
*   **Modular Design:** The solution is broken down into several projects, each handling a specific part of the workflow:
    *   `CSIAutoPlanner`: Handles the specifics of Craniospinal Irradiation planning.
    *   `TBIAutoPlanner`: Manages Total Body Irradiation workflows.
    *   `TMLIAutoPlanner`: Contains the logic for Total Marrow and Lymphoid Irradiation.
    *   `AutoPlannerOptimizationLoop`: A dedicated tool for running plan optimizations.
    *   `CTStitcher`: A utility for stitching CT scans.
    *   `ImportListener`: A service that listens for new patient data imports.
*   **DICOM Integration:** The project uses libraries like `EvilDICOM` and `SimpleITK` to handle medical imaging data.

## Project Structure

The main application logic is contained within the `VMATTXICSIAutoPlanner` directory, which is a Visual Studio Solution (`.sln`).

*   `/VMATTXICSIAutoPlanner`: Root directory for the Visual Studio solution.
    *   `/AutoPlannerLauncher`: The main entry point and launcher for the various tools.
    *   `/CSIAutoPlanner`, `/TBIAutoPlanner`, `/TMLIAutoPlanner`: Individual applications for each treatment modality.
    *   `/AutoPlannerOptimizationLoop`: The core optimization engine.
    *   `/CTStitcher`: The CT stitching utility.
    *   `/AutoPlannerHelpers`: A shared library with common code and utilities used across the different modules.
    *   `/packages`: Contains the NuGet package dependencies.

## Getting Started

### Prerequisites

*   Visual Studio 2019 or later
*   .NET Framework (check project properties for the specific version, likely 4.6.1 or higher)
*   Access to a clinical treatment planning system (for data and validation)

### Building the Project

1.  Clone the repository to your local machine.
2.  Open `VMATTXICSIAutoPlanner/VMATTXICSIAutoPlanner.sln` in Visual Studio.
3.  Restore the NuGet packages (this should happen automatically, but you can right-click the solution in Solution Explorer and select "Restore NuGet Packages").
4.  Build the solution (Build > Build Solution).

### Running the Application

After a successful build, the main executable can be found in the `AutoPlannerLauncher/bin/Debug` (or `Release`) directory. Run `AutoPlannerLauncher.exe` to start the application.

## Documentation

Detailed user guides and technical documentation can be found in the `/Documentation` directory:

*   `VMAT-CSI_PrepScript_QuickStartGuide.pdf`
*   `VMAT-TBI-CSI_OptLoop_Guide.pdf`
*   `VMAT-TBI-CSI_OptLoop_QuickStartGuide.pdf`
*   `Planning Instructions for VMAT TMLI.docx`
