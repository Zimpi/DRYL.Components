using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class LiveVoiceTests(BrowserFixture browser)
{
    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task Live_delegation_runs_through_real_Blazor_and_drains_final_events(string mode) =>
        browser.RunAsync(nameof(Live_delegation_runs_through_real_Blazor_and_drains_final_events), mode, async page =>
        {
            await page.Locator("#live-start").ClickAsync();
            await Phase(page, "Connecting");
            await page.WaitForFunctionAsync("() => voiceFixture.pending().some(p => p.stage === 'media')");
            await page.EvaluateAsync("() => voiceFixture.completeMedia()");
            await page.WaitForFunctionAsync("() => voiceFixture.connection()?.channelState === 'open'");
            Assert.Equal("fixture-live-answer", await page.EvaluateAsync<string>("() => voiceFixture.connection().remoteSdp"));
            Assert.Equal(0, await page.EvaluateAsync<int>("() => voiceFixture.stats().calls.fetch"));
            await Phase(page, "Connecting");
            Assert.Equal(0, await page.EvaluateAsync<int>("() => voiceFixture.sent().length"));
            await Emit(page, """{type:'session.started'}""");
            await Phase(page, "Live");

            await Emit(page, """{type:'session.input_transcript.delta',delta:'Hallo ',start_ms:0,end_ms:100}""");
            await Emit(page, """{type:'session.output_transcript.delta',delta:'Ich ',start_ms:50,end_ms:140}""");
            await Emit(page, """{type:'session.input_transcript.delta',delta:'Jan',start_ms:100,end_ms:200}""");
            await Emit(page, """{type:'session.output_transcript.delta',delta:'suche.',start_ms:140,end_ms:200}""");
            await Assertions.Expect(page.Locator("#live-deltas")).ToHaveTextAsync("4");
            await Assertions.Expect(page.Locator("#live-user-caption")).ToHaveTextAsync("Hallo Jan");
            await Assertions.Expect(page.Locator("#live-assistant-caption")).ToHaveTextAsync("Ich suche.");

            await Backend(page, """{type:'response.created',response:{id:'r_lookup'}}""");
            await Backend(page, """{type:'response.output_item.done',output_index:0,item:{id:'item_lookup',type:'function_call',call_id:'call_lookup',name:'fixture_lookup',arguments:'{"query":"Blazor"}'}}""");
            await Assertions.Expect(page.Locator("#live-tool-count")).ToHaveTextAsync("0");
            await Backend(page, """{type:'response.completed',response:{id:'r_lookup',status:'completed',output:[]}}""");
            await Assertions.Expect(page.Locator("#live-tool-count")).ToHaveTextAsync("1");
            await Assertions.Expect(page.Locator("#live-tool-trace")).ToHaveTextAsync("1");
            await page.WaitForFunctionAsync("() => voiceFixture.sent().length === 2");
            Assert.Equal("response.item.create,response.create", await page.EvaluateAsync<string>("() => voiceFixture.sent().map(e => e.type).join(',')"));
            Assert.Equal("found:Blazor", await page.EvaluateAsync<string>("() => JSON.parse(voiceFixture.sent()[0].item.output).value"));
            Assert.Equal("call_lookup", await page.EvaluateAsync<string>("() => voiceFixture.sent()[0].item.call_id"));
            Assert.False(await page.EvaluateAsync<bool>("() => 'delegation_id' in voiceFixture.sent()[1] || 'model' in voiceFixture.sent()[1] || 'response' in voiceFixture.sent()[1]"));
            await Backend(page, """{type:'response.completed',response:{id:'r_lookup',status:'completed',output:[]}}""");

            await Backend(page, """{type:'response.created',response:{id:'r_search'}}""");
            await Backend(page, """{type:'response.output_item.done',output_index:0,item:{id:'web_result',type:'web_search_call',status:'completed'}}""");
            await Backend(page, """{type:'response.output_item.done',output_index:1,item:{id:'cited_message',type:'message',content:[{type:'output_text',text:'Cited answer.',annotations:[{type:'url_citation',url:'https://example.com/source',title:'Fixture source'}]}]}}""");
            await Backend(page, """{type:'response.completed',response:{id:'r_search',status:'completed',output:[],usage:{total_tokens:23}}}""");
            await Assertions.Expect(page.Locator("#live-backend-count")).ToHaveTextAsync("2");
            await Assertions.Expect(page.Locator("#live-backend-result")).ToContainTextAsync("https://example.com/source");
            await Assertions.Expect(page.Locator("#live-tool-count")).ToHaveTextAsync("1");
            await Assertions.Expect(page.Locator("#live-deltas")).ToHaveTextAsync("4");

            await page.Locator("#live-stop").ClickAsync();
            await Phase(page, "Closing");
            Assert.Equal(0, await page.EvaluateAsync<int>("() => voiceFixture.stats().liveTracks"));
            Assert.Equal(1, await page.EvaluateAsync<int>("() => voiceFixture.stats().livePeers"));
            Assert.Equal("session.close", await page.EvaluateAsync<string>("() => voiceFixture.sent().at(-1).type"));
            await Emit(page, """{type:'session.output_transcript.delta',delta:' Bis bald.',start_ms:250,end_ms:500}""");
            await Emit(page, """{type:'session.usage.updated',usage:{seconds:11}}""");
            await Assertions.Expect(page.Locator("#live-usage")).ToHaveTextAsync("11");
            await Emit(page, """{type:'session.closed',usage:{seconds:12.5},reason:'close_requested'}""");
            await Phase(page, "Idle");
            await Assertions.Expect(page.Locator("#live-finalized")).ToHaveTextAsync("true");
            await Assertions.Expect(page.Locator("#live-close-reason")).ToHaveTextAsync("close_requested");
            await Assertions.Expect(page.Locator("#live-usage")).ToHaveTextAsync("12.5");
            await Assertions.Expect(page.Locator("#live-deltas")).ToHaveTextAsync("5");
            await Assertions.Expect(page.Locator("#live-assistant-caption")).ToHaveTextAsync("Ich suche. Bis bald.");
            await Assertions.Expect(page.Locator("#live-error")).ToBeEmptyAsync();
            await page.WaitForFunctionAsync("() => { const s=voiceFixture.stats(); return ['liveTracks','livePeers','liveChannels','liveContexts','audioElements','timers','frames'].every(k=>s[k]===0); }");
        });

    private static Task Phase(IPage page, string value) => Assertions.Expect(page.Locator("#live-phase")).ToHaveTextAsync(value);
    private static Task Emit(IPage page, string expression) => page.EvaluateAsync("() => voiceFixture.emit(" + expression + ")");
    private static Task Backend(IPage page, string expression) => Emit(page, "{type:'response.event',delegation_id:'delegation_fixture',event:" + expression + "}");
}
