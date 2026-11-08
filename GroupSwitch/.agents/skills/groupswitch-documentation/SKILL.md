---
name: groupswitch-documentation
description: Create or update GroupSwitch architecture, UML, Merise, database, API, installation, user and maintenance documentation. Use when documenting implemented behavior, design decisions, operating procedures or project deliverables.
---

# Documentation Workflow

1. Read AGENTS.md, the project specification, existing documentation,
   and the implementation relevant to the requested scope.

2. Inventory existing documents.
   Reuse their organization before creating new documents.
   Identify outdated content and missing information.

3. Establish the documentation status of every feature:
    - required;
    - implemented;
    - proposed;
    - verified;
    - unverified.

4. Ground statements in source files, configuration, migrations,
   tests, or actual execution evidence.
   Never invent commands, tables, endpoints, relationships,
   screenshots, or successful checks.

5. Write concise technical documentation in English.
   Use consistent domain terminology.

6. Create diagrams only when they clarify a useful relationship.
   Keep diagram sources editable.

7. For architecture, document:
    - system boundaries;
    - frontend and backend responsibilities;
    - project dependencies;
    - external integrations;
    - important runtime flows;
    - deployment assumptions.

8. For UML, choose relevant diagrams:
    - use cases;
    - focused domain class diagrams;
    - sequences for important workflows;
    - state diagrams for request lifecycles;
    - component or deployment diagrams when useful.

9. For database documentation:
    - inspect entities, EF configuration, and migrations;
    - distinguish conceptual, logical, and physical models;
    - document keys, cardinalities, optionality, constraints,
      indexes, deletion rules, and academic scope;
    - identify the source of each relationship;
    - distinguish the migration model from a live database inspection.

10. For Merise:
    - create a conceptual data model with explicit cardinalities;
    - derive the logical relational model;
    - describe the implemented physical model;
    - label approximations when the rendering format does not
      support conventional Merise notation.

11. For user and maintenance guides:
    - separate Student and Admin workflows;
    - document prerequisites and configuration;
    - use secret-free examples;
    - describe known failures and recovery procedures;
    - do not claim backup or restoration procedures were tested
      unless they were actually executed.

12. Check links, file paths, terminology, and consistency.
    Render or validate diagrams when tooling is available.
    State which checks could not be performed.

13. Return:
    - documents created or updated;
    - evidence used;
    - diagrams produced;
    - checks executed;
    - unresolved questions and limitations.
