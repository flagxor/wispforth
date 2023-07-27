all: out/wisp

out/:
	mkdir -p $@

out/wisp: wisp.fs | out/
	./wisp.fs

clean:
	rm -rf out/
