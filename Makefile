all: out/wisp

out/:
	mkdir -p $@

out/wisp: wisp.fs | out/
	./wisp.fs
	ls -lh out/wisp

clean:
	rm -rf out/
