# FixCSV ReadMe

I wrote this command line utility to solve a pressing need to coerce a CSV file
thhat otherwise conformed to RFC 4180 so that it can be imported into Microsoft
Excel as a table.

## Release Notes, 01/02/2021 17:53:33

This release adds counts of line break characters in the input and output data.

Input File:  CR Count = 0
             LF Count = 1,763
Output File: CR Count = 0
             LF Count = 1,731

You should expect the output CR count to always be zero, because the CR
characters are inserted when the text is written into the output file.
Internally, line breaks are represented by line feed (LF) characters, that being
sufficient to denote a line break.

Observant installers may notice that the NuGet packages are one commit behind
the NuGet Gellery. This came about because the NuGet Package Manager in Visual
Studio 2019 threw a null reference exception when I asked it to update the
installed packages that were out of date.

Since nothing in the newer releases affects this program, the fact that they are
one release behind the NuGet Gallery is inconsequential.

## Release Notes, 01/01/2021 13:24:03

In my new day job, I encountered many CSV files that conform to RFC 4180, but
are problematic for importing into Microsoft Excel for two reasons.

1.  They contain fields that have embedded line breaks. These make a royal mess
of the resulting Excel worksheet.

2.  They contain extra line breaks, frequently nonstandard, such as, for example,
a standard Windows line break followed immediately by a standard Unix line break.
On import, the result is that every other row is empty. Though a macro can clean
up the mess, it made more sense to me to write and use a specialized utility
program.

## Contributing

Though I created this project to meet my individual development needs, I have
put a good bit of thought and care into its design. Moreover, since I will not
live forever, and I want these programs to outlive me, I would be honored to
add contributions from others to it. My expectations are few, simple, easy to
meet, and intended to maintain the consistency of the code base.

1.  __Naming Conventions__: I use Hungarian notation. Some claim that it has
outlived its usefulness. I think it remains useful because it encodes data
about the objects to which the names are applied that follows them wherever they
go, and convey it without help from IntelliSense.

2.  __Coding Style__: I have my editor set to leave spaces around every token.
This spacing has helped me quicly spot misplaced puncuation, such as the right
bracket that closes an array initializer that is in the wrong place, and it
makes mathematical expressions easier to read and mentally parse.

3.  __Comments__: I comment liberally and very deliberately. Of particular
importance are the comments that I append to the bracket that closes a block. It
does either or both of two things: link it to the opening statement, and
document which of two paths an __if__ statement is expected to follow most of
the time. When blocks get nested two, three, or four deep, they really earn
their keep.

4.  __Negative Conditions__: I do my best to avoid them, because they almost
always cause confusion. Astute observers will notice that they occur from time
to time, because there are _a few cases_ where they happen to be less confusing.

5.  __Array Initializers__: Arrays that have more than a very few initializers,
or that are initialized to long strings, are laid out as lists, with line
comments, if necessary, that describe the intent of each item.

6.  __Format Item Lists__: Lists of items that are paired with format items in
calls to `string.Format`, `Console.WriteLine`, and their relatives, are laid out
as arrays, even if there are too few to warrant one, and the comments show the
corresponding format item in context. This helps ensure that the items are
listed in the correct order, and that all format items are covered.

7.  __Symbolic Constants__: I use symbolic constants to document what a literal
value means in the context in which it appears, and to disambiguate tokens that
are easy to confuse, suzh as `1` and `l` (lower case L), `0` and `o` (lower case O),
literal spaces (1 and 2 spaces are common), underscores, the number `-1`, and so
forth. Literals that are widely applicable are defined in a set of classes that
comprise the majority of the root `WizardWrx` namespace.

8.  __Argument Lists__: I treat argument lists as arrays, and often comment each
argument with the name of the paramter that it represents. These lists help
guarantee that a long list of positional arguments is specified in the correct
order, especially when several are of the same type (e. g., two or more integer
arguments).

9.  __Triple-slash Comments__: These go on _everything_, even private members and
methods, so that everything supports IntelliSense, and it's easy to apply a tool
(I use DocFX.) to generate reference documentation.

With respect to the above items, you can expect me to be a nazi, though I shall
endeavor to give contributors a fair hearing when they have a good case.
Otherwise, please exercise your imagination, and submit your pull requests.
If you skim the headnotes of the code, you'll see that I take great pains to
give others credit when I icorporate their code into my projects, even to the
point of disclaiming copyright or leaving their copyright notice intact. Along
the same lines, the comments are liberally sprinkled with references to articles
and Stack Overflow discussions that contributed to the work.