Welcome to Sprint Report Generator!
---------------------------------

A simple tool that allows to generate a report of the percentage worked in each task during a Scrum sprint, from a txt with a simple formatting.

#### Download & Installation

For now the only way is to clone the repo and compile it, sorry :(  
You'll need Visual Studio 2012.

We could add a compiled version if we see fit.

#### Usage

`SprintReportGenerator.Console <taskFile> <Developer Name>`

### Supported syntax

Basically you can write your notes as you want, with some little considerations:  
- Text inside "[..]" will be considered as metadata for the SprintReportGenerator  
- The basic structure must be Sprint > Days > Tasks
- Excepting days, only lines with metadata blocks will be considered.

- Sprint start day format: `YYYY-MM-DD [SPRINT:<SprintNumber>]`
  - You can include any data in the line as far as you put the day first and include the sprint metadata block somewhere.  
  - Example:
    - `2015-02-23 - [SPRINT:53]`
    - `2015-02-23 - This is gonna be a great [SPRINT:53] !!`

- Day format: `YYYY-MM-DD`
  - You can write it however you want as far as you put the day first.
  - Example: 
    - `2015-02-24`
    - `2015-02-24 Great day for debugging!`

- DayOff format: `YYYY-MM-DD [DAYOFF:<DayOff Description>]`
  - You can write it however you want as far as you put the day first and the dayOff metadata block somwhere.
  - DayOff description is optional.
  - Example: 
    - `2015-02-24 [DAYOFF]`
    - `2015-02-24 [DAYOFF:Moving to Paris]`
    - `2015-02-24 Great day for running away! [DAYOFF:Moving to Paris] :)`

- Task format: `[<TaskType>:Description:<Task Percentage>]`
  - TaskType is optional, a default of F will be used if not present.
  - Task percentage is optional, if not present it will calculated from the remaining percentage of the day.
    - If only one task is present, it will have 100%.
    - You could indicate the percentage of only one task, and the remaining for the day will be equally distributed among the remaning tasks that don't have percentages specified.     
  - Example:
    - `[This is an example feature]`
    - `[F:This is an example feature]`
    - `[B:This is an example bug]`
    - `[L:I'm leaving for half the day:50]`
    - `[O:This in an example task that took my 70%:70]`

TODO: Add example task notes file.

### Validations

The tool validates a lots of things and tries to print useful messages.  
TODO: Include list of validations.

### Extra notes

The whole file is parsed into rich Sprint, Day and Task models, so any logic can be easily be added.

### Bugs, contact & contributions

Just file an issue on GitHub or send a PR!!

### License

Read `LICENSE` 

