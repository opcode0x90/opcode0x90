bin_PROGRAMS = hlping
hlping_SOURCES = hlping.cpp hlds_client.cpp

# dependencies
hlping_CXXFLAGS = $(BOOST_CPPFLAGS)
hlping_LDFLAGS = $(BOOST_LDFLAGS) -lpthread \
    $(BOOST_PROGRAM_OPTIONS_LIB) \
    $(BOOST_ASIO_LIB)
