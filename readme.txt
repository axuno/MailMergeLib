!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
Heads up for MailMergeLib 5.11
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

This version is API and source compatible with prior 5.x versions.

In v5.11 the referenced package for SmartFormat.NET is updated
from v2.7.3 to v3.2.1. This new major version of SmartFormat.NET incurs
breaking changes. MailMergeLib manages breaking API changes
under the hood. Other breaking changes are related to using formatters
in placeholders:

(1) If you're only using plain placeholders like "{Email}" or even "{Today:yyyy-MM-dd}" 
    there's no need for updating the format strings, and you're fine.

(2) If you're using formatters like "{Fruit:cond:Apple|Pie|Orange|Banana|No fruit}",
    where the rendered string depends on the Fruit variable, urgently have a look at
    https://github.com/axuno/SmartFormat/wiki/Migration#2-formatter-differences-from-v2-to-v3
    Required modifications are not extensive, but still necessary.

(3) On the other side SmartFormat v3 has many advantages:
    * Parsing is 10% faster with 50-80% less GC and memory allocation
    * Formatting is up to 40% faster with 50% less GC and memory allocation
    * Nullable notation inside placeholders
    See more details here: https://github.com/axuno/SmartFormat/wiki/Why-Migrate
