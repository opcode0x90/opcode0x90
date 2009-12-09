AC_PREREQ([2.13])
AC_INIT([hlping], [0.0.1a], [opcode0x90@hotmail.com])

AC_CONFIG_SRCDIR([src])
AC_CONFIG_AUX_DIR([build])
AC_CONFIG_MACRO_DIR ([build/m4])

AM_INIT_AUTOMAKE([-Wall -Werror foreign])
AC_PROG_CXX


# dependencies
AX_BOOST_BASE([1.33.1])
AX_BOOST_ASIO
AX_BOOST_PROGRAM_OPTIONS

# config files
#AC_CONFIG_HEADERS([config.h])
AC_CONFIG_FILES([
    Makefile
    src/Makefile
])

AC_OUTPUT
