# HarmonizeGit
A tool for managing multiple git repositories.   Geared towards users who want to code a library in parallel with its user program, 
while avoiding putting both in a single repo, utilizing submodules, or versioning tools like nuget.   Think of it like submodules without 
the re-cloning. 

# Inspiration
In my typical workflow, I use a lot of internal reusable libraries.  Most of them are developed in parallel with the end product, while also
being developed in other end products at the same time.  Very often, while developing an end product, a single line or function is added
to the reusable libraries.  I might then commit and swap immediately to a different end product, which may want to use this new function too,
or wait until a later time to utilize and merge in that version of the common library.  This workflow challenges the typical repository setups:
*  **Different Repositories** If the common library and end project are in different repositories, then a version manager like NuGet or Maven needs to be utilized to sync.  This requires even minor tweaks in the library to be stamped with a new version number and published, and then pulled into the end program.  This setup makes sense when the library developer is 3rd party, or even a different division in a company.  It makes less sense when you are the developer for both and are developing in parallel.
* **Super Repository** If the common library and the end project(s) are in the same repository, then you end up with many end projects and libraries all in one super-repository.  This comes with many downsides such as very complex branching and merging.  The plus is that both end project and library can be modified in parallel, and the exact state of both are saved at the time of the commit.
* **Submodules** are a decent solution that breaks down with more complex setups.  If you have several separate libraries that are used by an end product, it can work well.  If an end project uses library A and B, where library B also uses A, then the submodule setup breaks down because each submodule requires its own clone of library A.  When you're constructing your workspace, which library A project do you include and work on?  The end project's clone of A, or library B's clone of A?  This multiplies with your library usage complexity.

I wanted a setup very similar to submodules, without requiring the reclone.  This would mean projects could be split into many repositories, but each would only have one clone on disk, no matter who used who.  They would maintain sync, and checking out one commit in a user program's repository would be able to check out the commit in the parent repository it was using at the time.

# User Experience
The user can clone several repositories of several libraries they are using, but load each into a single solution with their end project.  The user can work on their end-project, and any number of their libraries as needed.  When it comes time to commit, all modified files are already sorted into their appropriate repositories.  The user will commit in the library repositories and then in the end project repository.  The entire setup can now be restored later.

If a month later, the user checks out the commit we just discussed, the appropriate commits in all the parent repositories will be checked out as well to bring the entire setup to the state it was at the time of the commits.  In this way, the setup acts as a single repository when needed, while being separate as far as pushing/pulling/working with other repositories.

I've already swapped to use Harmonize Git in my own hobby repositories.  HarmonizeGit's repository itself is already greedily using itself, and syncing with FishingWithGit.

# Implementation Components
**Harmonize Config**
A file named ".harmonize" is added to the working directory and is committed.  It has XML specifying the parent repositories it depends on.  It also will be used by the system to then track which commit each parent repository was on at the time of committing.

**Pathing File**
Unlike the harmonize config file, the pathing file is not committed.  It is automatically generated on first usage.  It provides a customization area so the user can specify where each parent repository is located on disk, if it happens to be abnormal or different from other cloners.  If all repositories are cloned to the same base directory, then this file can be left as default.

**Child Usage DB**
A SQLite file is automatically created for parent repositories.  This is populated with child repository usage in order to provide safety features.

**[Fishing With Git](https://github.com/Noggog/FishingWithGit)**
This is another project I've developed to provide a suite of additional git hooks.  HarmonizeGit uses these additional hooks to activate and provide its functionality.

**Exe Hook**
An .exe program is put in each repository's hooks folder.  This provides the logic for how to maintain sync.  It executed by Fishing With Git.

# Typical Use Cases
**Child Repository Commits**
If any parent repository has uncommitted changes, the commit is blocked and the user is warned.  If the parent repositories are clean, then the harmonize config is updated with their current shas and committed alongside.

**Child Repository Checks Out an Old Commit**
Again, if parent repos have uncommitted changes, it is blocked.  Otherwise, the checkout executes, and the harmonize config is consulted and the appropriate commits are checkout out in the parent repos.  New branches are made if needed.

**Parent Repo Resets or Otherwise Loses Commit References**
The offending reset, rebase, etc is blocked if children repositories are referencing the would-be-lost commits.  The children repositories must delete their referring commits before the parent can execute.

**Parent Repo Checks Out A Different Commit**
The child repository's harmonize config is updated to the new sha to match.  This will be an uncommitted change in that repository.

**Child Repository Discards Uncommitted Harmonize Config Change**
The parent repository checks out the config left over after the discard.

**Other**
There are many other edge cases, but these should outline the basic gist of what Harmonize Git intends to accomplish.
