# Requirements Document

## Introduction

This document defines the requirements for SmartMacroAI Advanced Features V2 — a comprehensive feature set that extends the WPF .NET 8 desktop macro automation tool with onboarding, community sharing, enhanced vision capabilities, parallel execution, and visual scripting. These features collectively transform SmartMacroAI from a power-user tool into an accessible, community-driven automation platform.

## Glossary

- **Tutorial_Wizard**: The first-run guided onboarding experience that introduces new users to SmartMacroAI's core features through interactive steps
- **Template_Library**: The service that manages pre-built macro templates organized by game or application category
- **Marketplace_Service**: The backend service enabling users to upload, browse, download, rate, and share macro scripts with the community
- **Screenshot_Logger**: The subsystem that automatically captures window screenshots when errors occur during macro execution
- **AI_Vision_Engine**: The ML-based image recognition subsystem that uses trained models (ONNX Runtime) to detect objects, UI elements, and game states beyond simple template matching
- **Parallel_Executor**: The subsystem that manages concurrent macro execution across multiple target windows or application instances
- **Visual_Script_Editor**: The drag-and-drop node-based editor that allows users to compose macro workflows visually without writing JSON or code
- **Macro_Script**: A serialized workflow definition containing ordered actions, conditions, and metadata
- **ONNX_Model**: An Open Neural Network Exchange model file used by the AI_Vision_Engine for inference
- **Node_Graph**: A visual representation of a macro workflow where actions are nodes connected by edges representing execution flow

## Requirements

### Requirement 1: First-Run Tutorial Wizard

**User Story:** As a new user, I want a guided onboarding experience when I first launch SmartMacroAI, so that I can quickly understand the core features without reading external documentation.

#### Acceptance Criteria

1. WHEN SmartMacroAI launches for the first time on a machine, THE Tutorial_Wizard SHALL display a multi-step guided walkthrough covering: target window selection, action creation, macro execution, and template loading
2. WHILE the Tutorial_Wizard is active, THE Tutorial_Wizard SHALL highlight the relevant UI element for the current step using a spotlight overlay
3. WHEN the user clicks "Next" on a tutorial step, THE Tutorial_Wizard SHALL advance to the subsequent step and update the spotlight position within 200ms
4. WHEN the user clicks "Skip Tutorial", THE Tutorial_Wizard SHALL close immediately and mark the tutorial as completed in user preferences
5. THE Tutorial_Wizard SHALL persist tutorial completion state to a local settings file so the wizard does not reappear on subsequent launches
6. WHEN the user navigates to Settings and clicks "Restart Tutorial", THE Tutorial_Wizard SHALL reset the completion state and display the wizard on next app launch
7. THE Tutorial_Wizard SHALL support both Vietnamese and English localization, matching the application's current language setting

### Requirement 2: Template Macros for Popular Games

**User Story:** As a gamer, I want pre-built macro templates for popular games, so that I can start automating repetitive tasks without building macros from scratch.

#### Acceptance Criteria

1. THE Template_Library SHALL provide at least 10 pre-built macro templates organized into categories: MMORPG, MOBA, FPS, Idle/Clicker, and Web Automation
2. WHEN the user selects a template from the Template_Library, THE Template_Library SHALL load the template actions into the macro editor with placeholder variables clearly marked using double-brace syntax (e.g., `{{target_window}}`)
3. WHEN a template contains image-based actions, THE Template_Library SHALL include reference screenshots in a bundled assets folder alongside the template definition
4. THE Template_Library SHALL store templates as JSON files in a `templates/` directory, using the same serialization format as user-created Macro_Scripts
5. WHEN the user modifies a loaded template, THE Template_Library SHALL save the modified version as a new user script without altering the original template file
6. THE Template_Library SHALL display each template with: name, description, target game/application, difficulty level, and estimated setup time

### Requirement 3: Community Marketplace

**User Story:** As a macro creator, I want to share my macros with other users and discover macros built by the community, so that I can contribute to and benefit from collective automation knowledge.

#### Acceptance Criteria

1. WHEN the user clicks "Publish to Marketplace", THE Marketplace_Service SHALL upload the selected Macro_Script with metadata (name, description, category, tags, author) to the community server
2. WHEN the user browses the Marketplace, THE Marketplace_Service SHALL display available macros with: name, author, download count, average rating, category, and last-updated date
3. WHEN the user clicks "Download" on a marketplace macro, THE Marketplace_Service SHALL download the Macro_Script and save it to the user's local scripts folder
4. WHEN the user rates a downloaded macro, THE Marketplace_Service SHALL submit the rating (1-5 stars) and update the displayed average rating
5. IF the community server is unreachable, THEN THE Marketplace_Service SHALL display a connection error message and allow the user to retry or work offline
6. THE Marketplace_Service SHALL validate uploaded macros for structural integrity before accepting the upload, rejecting scripts that fail JSON schema validation
7. WHEN the user searches the Marketplace, THE Marketplace_Service SHALL return results matching the query against macro name, description, tags, and author within 3 seconds

### Requirement 4: Screenshot Log on Error

**User Story:** As a macro developer, I want automatic screenshots captured when errors occur during macro execution, so that I can diagnose failures without manually reproducing them.

#### Acceptance Criteria

1. WHEN an unhandled exception occurs during macro execution, THE Screenshot_Logger SHALL capture a screenshot of the target window within 500ms of the error
2. WHEN a screenshot is captured on error, THE Screenshot_Logger SHALL save the image as a PNG file in a `screenshots/errors/` directory with filename format `error_{scriptName}_{timestamp}.png`
3. WHEN a screenshot is captured on error, THE Screenshot_Logger SHALL log the error details (exception type, message, action index, timestamp) alongside the screenshot path in the run history
4. THE Screenshot_Logger SHALL limit stored error screenshots to a configurable maximum count (default: 100), deleting the oldest screenshots when the limit is exceeded
5. WHEN the user opens the Run History panel, THE Screenshot_Logger SHALL display error entries with a clickable thumbnail that opens the full-size screenshot
6. IF the target window is not accessible at the time of error, THEN THE Screenshot_Logger SHALL capture a full-screen screenshot as a fallback and annotate the log entry accordingly

### Requirement 5: AI Image Recognition

**User Story:** As an advanced user, I want AI-powered image recognition that can detect objects and UI states beyond exact template matching, so that my macros are more resilient to visual changes like resolution scaling, color themes, and minor UI updates.

#### Acceptance Criteria

1. THE AI_Vision_Engine SHALL support loading ONNX_Model files for object detection and classification inference
2. WHEN the user configures an image-match action with AI mode enabled, THE AI_Vision_Engine SHALL use the loaded ONNX_Model to detect the target object and return bounding box coordinates with a confidence score
3. THE AI_Vision_Engine SHALL fall back to template matching (Emgu.CV CcoeffNormed) when no ONNX_Model is loaded or when AI detection confidence is below the user-configured threshold
4. WHEN the AI_Vision_Engine detects a target, THE AI_Vision_Engine SHALL return results within 500ms per frame on hardware with a DirectX 12-capable GPU
5. THE AI_Vision_Engine SHALL support running inference on CPU when no compatible GPU is available, with a maximum latency of 2000ms per frame
6. WHEN the user trains a custom model, THE AI_Vision_Engine SHALL accept a folder of labeled screenshots and produce an ONNX_Model file using transfer learning from a pre-trained base model
7. THE AI_Vision_Engine SHALL expose a confidence threshold setting (0.0 to 1.0) per action, allowing users to tune detection sensitivity

### Requirement 6: Multi-Window Parallel Execution

**User Story:** As a multi-account user, I want to run the same macro simultaneously on multiple game windows, so that I can automate repetitive tasks across all my accounts in parallel.

#### Acceptance Criteria

1. WHEN the user selects multiple target windows and clicks "Run All", THE Parallel_Executor SHALL launch independent macro execution instances for each selected window concurrently
2. WHILE multiple macros are executing in parallel, THE Parallel_Executor SHALL isolate each instance's variable state, timing, and error handling so that a failure in one instance does not affect others
3. THE Parallel_Executor SHALL display a dashboard showing the status (Running, Paused, Error, Completed) of each parallel execution instance with the target window title
4. WHEN the user clicks "Stop All", THE Parallel_Executor SHALL send cancellation signals to all running instances and confirm termination within 2 seconds
5. THE Parallel_Executor SHALL limit concurrent execution instances to a configurable maximum (default: 8) to prevent system resource exhaustion
6. WHEN a parallel instance encounters an error, THE Parallel_Executor SHALL log the error for that instance and continue executing the remaining instances without interruption
7. THE Parallel_Executor SHALL use the existing Win32Api.PostMessage-based stealth input so that parallel instances do not interfere with each other's target windows

### Requirement 7: Drag & Drop Visual Scripting

**User Story:** As a non-technical user, I want a visual node-based editor where I can drag and drop actions to build macros, so that I can create complex automation workflows without understanding JSON or code.

#### Acceptance Criteria

1. THE Visual_Script_Editor SHALL display macro actions as draggable nodes on a canvas, with input/output connection ports representing execution flow
2. WHEN the user drags a node from the action palette onto the canvas, THE Visual_Script_Editor SHALL create a new action node at the drop position and add it to the Node_Graph
3. WHEN the user draws a connection line between two node ports, THE Visual_Script_Editor SHALL establish an execution flow edge between the source and target nodes
4. THE Visual_Script_Editor SHALL serialize the Node_Graph into the same JSON format used by the existing Macro_Script model, ensuring full compatibility with the text-based editor
5. WHEN the user opens an existing Macro_Script in the Visual_Script_Editor, THE Visual_Script_Editor SHALL parse the JSON and render the corresponding Node_Graph with automatic layout within 2 seconds for scripts containing up to 50 actions
6. WHEN the user deletes a node, THE Visual_Script_Editor SHALL remove the node and all connected edges, then update the underlying Macro_Script accordingly
7. THE Visual_Script_Editor SHALL support undo/redo operations for all canvas modifications with a history depth of at least 50 operations
8. THE Visual_Script_Editor SHALL visually distinguish node types using color coding: blue for input actions, green for flow control, orange for vision actions, and purple for web actions
9. WHEN the user double-clicks a node, THE Visual_Script_Editor SHALL open the existing ActionEditDialog for that action type, pre-populated with the node's current configuration
